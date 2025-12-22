using Microsoft.EntityFrameworkCore;
using KhairAPI.Data;
using KhairAPI.Models.DTOs;
using KhairAPI.Models.Entities;
using KhairAPI.Services.Interfaces;
using KhairAPI.Core.Helpers;

namespace KhairAPI.Services.Implementations
{
    public class StatisticsService : IStatisticsService
    {
        private readonly AppDbContext _context;

        public StatisticsService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync(int? teacherId = null)
        {
            var today = DateTime.UtcNow.Date;

            var studentQuery = _context.Students.AsQueryable();
            var progressQuery = _context.ProgressRecords.Where(p => p.Date == today);
            var attendanceQuery = _context.Attendances.Where(a => a.Date == today);

            if (teacherId.HasValue)
            {
                var studentIds = await _context.StudentHalaqat
                    .Where(sh => sh.TeacherId == teacherId && sh.IsActive)
                    .Select(sh => sh.StudentId)
                    .ToListAsync();

                studentQuery = studentQuery.Where(s => studentIds.Contains(s.Id));
                progressQuery = progressQuery.Where(p => p.TeacherId == teacherId);
                attendanceQuery = attendanceQuery.Where(a => studentIds.Contains(a.StudentId));
            }

            var totalStudents = await studentQuery.CountAsync();
            var totalTeachers = await _context.Teachers.CountAsync();
            var totalHalaqat = await _context.Halaqat.CountAsync();
            var activeHalaqat = await _context.Halaqat.Where(h => h.IsActive).CountAsync();

            var todayProgress = await progressQuery.ToListAsync();
            var todayAttendance = await attendanceQuery.CountAsync(a => a.Status == AttendanceStatus.Present);
            var totalTodayAttendance = await attendanceQuery.CountAsync();

            double averageAttendance = totalTodayAttendance > 0
                ? (double)todayAttendance / totalTodayAttendance * 100
                : 0;

            return new DashboardStatsDto
            {
                TotalStudents = totalStudents,
                TotalTeachers = totalTeachers,
                TotalHalaqat = totalHalaqat,
                ActiveHalaqat = activeHalaqat,
                AverageAttendanceRate = Math.Round(averageAttendance, 1),
                TodayMemorization = todayProgress.Count(p => p.Type == ProgressType.Memorization),
                TodayRevision = todayProgress.Count(p => p.Type == ProgressType.Revision),
                TodayAttendance = todayAttendance
            };
        }

        public async Task<ReportStatsDto> GetReportStatsAsync(string dateRange, int? halaqaId = null, int? teacherId = null)
        {
            var today = DateTime.UtcNow.Date;
            var fromDate = dateRange switch
            {
                "week" => today.AddDays(-7),
                "month" => today.AddMonths(-1),
                _ => DateTime.MinValue
            };

            var progressQuery = _context.ProgressRecords.Where(p => p.Date >= fromDate && p.Date <= today);
            var attendanceQuery = _context.Attendances.Where(a => a.Date >= fromDate && a.Date <= today);
            var studentQuery = _context.Students.AsQueryable();

            if (halaqaId.HasValue && halaqaId.Value > 0)
            {
                progressQuery = progressQuery.Where(p => p.HalaqaId == halaqaId);
                attendanceQuery = attendanceQuery.Where(a => a.HalaqaId == halaqaId);
                var halaqaStudentIds = await _context.StudentHalaqat
                    .Where(sh => sh.HalaqaId == halaqaId && sh.IsActive)
                    .Select(sh => sh.StudentId)
                    .ToListAsync();
                studentQuery = studentQuery.Where(s => halaqaStudentIds.Contains(s.Id));
            }

            if (teacherId.HasValue)
            {
                progressQuery = progressQuery.Where(p => p.TeacherId == teacherId);
                var teacherStudentIds = await _context.StudentHalaqat
                    .Where(sh => sh.TeacherId == teacherId && sh.IsActive)
                    .Select(sh => sh.StudentId)
                    .ToListAsync();
                studentQuery = studentQuery.Where(s => teacherStudentIds.Contains(s.Id));
                attendanceQuery = attendanceQuery.Where(a => teacherStudentIds.Contains(a.StudentId));
            }

            var progressRecords = await progressQuery.ToListAsync();
            var attendanceRecords = await attendanceQuery.ToListAsync();
            var totalStudents = await studentQuery.CountAsync();

            var progressData = new List<DailyChartDataDto>();
            var attendanceData = new List<DailyChartDataDto>();

            for (var date = fromDate; date <= today; date = date.AddDays(1))
            {
                var dayProgress = progressRecords.Where(p => p.Date == date).ToList();
                var dayAttendance = attendanceRecords.Where(a => a.Date == date).ToList();

                progressData.Add(new DailyChartDataDto
                {
                    Date = AppConstants.ArabicDayNames.GetDayName(date.DayOfWeek),
                    Memorization = dayProgress.Count(p => p.Type == ProgressType.Memorization),
                    Revision = dayProgress.Count(p => p.Type == ProgressType.Revision),
                    Rate = 0
                });

                var totalDayAttendance = dayAttendance.Count;
                var presentCount = dayAttendance.Count(a => a.Status == AttendanceStatus.Present);
                var rate = totalDayAttendance > 0 ? (double)presentCount / totalDayAttendance * 100 : 0;

                attendanceData.Add(new DailyChartDataDto
                {
                    Date = AppConstants.ArabicDayNames.GetDayName(date.DayOfWeek),
                    Memorization = 0,
                    Revision = 0,
                    Rate = Math.Round(rate, 1)
                });
            }

            var studentProgress = progressRecords
                .GroupBy(p => p.StudentId)
                .Select(g => new
                {
                    StudentId = g.Key,
                    Count = g.Count(),
                    AvgQuality = g.Average(p => (int)p.Quality)
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();

            var topStudentIds = studentProgress.Select(s => s.StudentId).ToList();
            var students = await _context.Students
                .Where(s => topStudentIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id);

            var topStudents = studentProgress.Select(sp => new StudentPerformanceDto
            {
                Name = students.TryGetValue(sp.StudentId, out var student)
                    ? $"{student.FirstName} {student.LastName}"
                    : "غير معروف",
                Progress = sp.Count,
                Quality = Math.Round(4 - sp.AvgQuality, 1)
            }).ToList();

            var qualityGroups = progressRecords
                .GroupBy(p => p.Quality)
                .Select(g => new { Quality = g.Key, Count = g.Count() })
                .ToList();

            var qualityDistribution = new List<QualityDistributionDto>
            {
                new() { Name = "ممتاز", Value = qualityGroups.FirstOrDefault(q => q.Quality == QualityRating.Excellent)?.Count ?? 0, Color = "#10B981" },
                new() { Name = "جيد جداً", Value = qualityGroups.FirstOrDefault(q => q.Quality == QualityRating.VeryGood)?.Count ?? 0, Color = "#3B82F6" },
                new() { Name = "جيد", Value = qualityGroups.FirstOrDefault(q => q.Quality == QualityRating.Good)?.Count ?? 0, Color = "#F59E0B" },
                new() { Name = "مقبول", Value = qualityGroups.FirstOrDefault(q => q.Quality == QualityRating.Acceptable)?.Count ?? 0, Color = "#EF4444" }
            };

            var totalAttendance = attendanceRecords.Count;
            var presentTotal = attendanceRecords.Count(a => a.Status == AttendanceStatus.Present);
            var avgAttendance = totalAttendance > 0 ? (double)presentTotal / totalAttendance * 100 : 0;
            var avgQuality = progressRecords.Any()
                ? Math.Round(4 - progressRecords.Average(p => (int)p.Quality) + 1, 1)
                : 0;

            return new ReportStatsDto
            {
                TotalStudents = totalStudents,
                AverageAttendance = Math.Round(avgAttendance, 1),
                WeeklyMemorization = progressRecords.Count(p => p.Type == ProgressType.Memorization),
                AverageQuality = avgQuality,
                ProgressData = progressData,
                AttendanceData = attendanceData,
                TopStudents = topStudents,
                QualityDistribution = qualityDistribution
            };
        }

        public async Task<SystemWideStatsDto> GetSystemWideStatsAsync()
        {
            var today = DateTime.UtcNow.Date;
            var yesterday = today.AddDays(-1);
            var weekStart = today.AddDays(-(int)today.DayOfWeek);

            var totalStudents = await _context.Students.CountAsync();

            var todayProgress = await _context.ProgressRecords.Where(p => p.Date == today).ToListAsync();
            var todayVersesMemorized = todayProgress
                .Where(p => p.Type == ProgressType.Memorization)
                .Sum(p => Math.Max(0, p.ToVerse - p.FromVerse + 1));
            var todayVersesReviewed = todayProgress
                .Where(p => p.Type == ProgressType.Revision)
                .Sum(p => Math.Max(0, p.ToVerse - p.FromVerse + 1));
            var todayStudentsActive = todayProgress.Select(p => p.StudentId).Distinct().Count();

            var yesterdayProgress = await _context.ProgressRecords.Where(p => p.Date == yesterday).ToListAsync();
            var yesterdayVersesMemorized = yesterdayProgress
                .Where(p => p.Type == ProgressType.Memorization)
                .Sum(p => Math.Max(0, p.ToVerse - p.FromVerse + 1));
            var yesterdayVersesReviewed = yesterdayProgress
                .Where(p => p.Type == ProgressType.Revision)
                .Sum(p => Math.Max(0, p.ToVerse - p.FromVerse + 1));
            var yesterdayStudentsActive = yesterdayProgress.Select(p => p.StudentId).Distinct().Count();

            var weekProgress = await _context.ProgressRecords
                .Where(p => p.Date >= weekStart && p.Date <= today)
                .ToListAsync();
            var weekVersesMemorized = weekProgress
                .Where(p => p.Type == ProgressType.Memorization)
                .Sum(p => Math.Max(0, p.ToVerse - p.FromVerse + 1));
            var weekVersesReviewed = weekProgress
                .Where(p => p.Type == ProgressType.Revision)
                .Sum(p => Math.Max(0, p.ToVerse - p.FromVerse + 1));
            var weekStudentsActive = weekProgress.Select(p => p.StudentId).Distinct().Count();

            return new SystemWideStatsDto
            {
                TodayVersesMemorized = todayVersesMemorized,
                TodayVersesReviewed = todayVersesReviewed,
                TodayStudentsActive = todayStudentsActive,
                TotalStudents = totalStudents,
                VersesMemorizedChange = todayVersesMemorized - yesterdayVersesMemorized,
                VersesReviewedChange = todayVersesReviewed - yesterdayVersesReviewed,
                StudentsActiveChange = todayStudentsActive - yesterdayStudentsActive,
                WeekVersesMemorized = weekVersesMemorized,
                WeekVersesReviewed = weekVersesReviewed,
                WeekStudentsActive = weekStudentsActive
            };
        }

        public async Task<SupervisorDashboardDto> GetSupervisorDashboardAsync()
        {
            var today = DateTime.UtcNow.Date;
            var weekAgo = today.AddDays(-7);

            var totalStudents = await _context.Students.CountAsync();
            var totalTeachers = await _context.Teachers.CountAsync();
            var totalHalaqat = await _context.Halaqat.Where(h => h.IsActive).CountAsync();

            var todayAttendance = await _context.Attendances.Where(a => a.Date == today).ToListAsync();
            var todayAttendanceRate = todayAttendance.Any()
                ? (double)todayAttendance.Count(a => a.Status == AttendanceStatus.Present) / todayAttendance.Count * 100
                : 0;

            var todayProgress = await _context.ProgressRecords.Where(p => p.Date == today).ToListAsync();
            var todayMemorization = todayProgress.Count(p => p.Type == ProgressType.Memorization);
            var todayRevision = todayProgress.Count(p => p.Type == ProgressType.Revision);

            var halaqatStats = await GetHalaqaRankingAsync(7, 5);
            var teacherStats = await GetTeacherRankingAsync(7, 5);
            var atRiskStudents = await GetAtRiskStudentsAsync(10);

            return new SupervisorDashboardDto
            {
                TotalStudents = totalStudents,
                TotalTeachers = totalTeachers,
                TotalHalaqat = totalHalaqat,
                TodayAttendanceRate = Math.Round(todayAttendanceRate, 1),
                TodayMemorization = todayMemorization,
                TodayRevision = todayRevision,
                StudentsAtRisk = atRiskStudents.Count,
                TopHalaqat = halaqatStats,
                TopTeachers = teacherStats,
                AtRiskStudents = atRiskStudents
            };
        }

        public async Task<List<HalaqaRankingDto>> GetHalaqaRankingAsync(int days = 7, int limit = 10)
        {
            var today = DateTime.UtcNow.Date;
            var fromDate = today.AddDays(-days);

            var halaqat = await _context.Halaqat
                .Where(h => h.IsActive)
                .Include(h => h.StudentHalaqat)
                .Include(h => h.HalaqaTeachers)
                .ToListAsync();

            var attendances = await _context.Attendances
                .Where(a => a.Date >= fromDate && a.Date <= today)
                .ToListAsync();

            var progressRecords = await _context.ProgressRecords
                .Where(p => p.Date >= fromDate && p.Date <= today)
                .ToListAsync();

            var rankings = halaqat.Select(h =>
            {
                var halaqaAttendance = attendances.Where(a => a.HalaqaId == h.Id).ToList();
                var halaqaProgress = progressRecords.Where(p => p.HalaqaId == h.Id).ToList();
                var studentCount = h.StudentHalaqat.Count(sh => sh.IsActive);

                var attendanceRate = halaqaAttendance.Any()
                    ? (double)halaqaAttendance.Count(a => a.Status == AttendanceStatus.Present) / halaqaAttendance.Count * 100
                    : 0;

                var totalVerses = halaqaProgress.Sum(p =>
                {
                    var verses = Math.Max(0, p.ToVerse - p.FromVerse + 1);
                    return p.Type == ProgressType.Memorization ? verses : verses * 0.5;
                });

                var versesPerStudent = studentCount > 0 ? totalVerses / studentCount : 0;
                var progressScore = Math.Min(versesPerStudent * 2, 40);
                var score = (attendanceRate * 0.6) + (progressScore * 0.4);

                return new HalaqaRankingDto
                {
                    Id = h.Id,
                    Name = h.Name,
                    StudentCount = studentCount,
                    TeacherCount = h.HalaqaTeachers.Count,
                    AttendanceRate = Math.Round(attendanceRate, 1),
                    WeeklyProgress = halaqaProgress.Count,
                    Score = Math.Round(score, 1)
                };
            })
            .OrderByDescending(r => r.Score)
            .Take(limit)
            .ToList();

            return rankings;
        }

        public async Task<List<HalaqaRankingDto>> GetTopHalaqatAsync()
        {
            return await GetHalaqaRankingAsync(7, 5);
        }

        public async Task<List<TeacherRankingDto>> GetTeacherRankingAsync(int days = 7, int limit = 10)
        {
            var today = DateTime.UtcNow.Date;
            var fromDate = today.AddDays(-days);

            var teachers = await _context.Teachers
                .Include(t => t.StudentHalaqat)
                .ToListAsync();

            var rankings = new List<TeacherRankingDto>();

            foreach (var teacher in teachers)
            {
                var studentIds = teacher.StudentHalaqat
                    .Where(sh => sh.IsActive)
                    .Select(sh => sh.StudentId)
                    .ToList();

                if (!studentIds.Any()) continue;

                var studentAttendance = await _context.Attendances
                    .Where(a => a.Date >= fromDate && a.Date <= today && studentIds.Contains(a.StudentId))
                    .ToListAsync();

                var teacherProgress = await _context.ProgressRecords
                    .Where(p => p.Date >= fromDate && p.Date <= today && p.TeacherId == teacher.Id)
                    .ToListAsync();

                var attendanceRate = studentAttendance.Any()
                    ? (double)studentAttendance.Count(a => a.Status == AttendanceStatus.Present) / studentAttendance.Count * 100
                    : 0;

                var avgQuality = teacherProgress.Any()
                    ? teacherProgress.Average(p => 4 - (int)p.Quality)
                    : 0;

                var totalVerses = teacherProgress.Sum(p =>
                {
                    var verses = Math.Max(0, p.ToVerse - p.FromVerse + 1);
                    return p.Type == ProgressType.Memorization ? verses : verses * 0.5;
                });

                var versesPerStudent = studentIds.Count > 0 ? totalVerses / studentIds.Count : 0;
                var progressScore = Math.Min(versesPerStudent * 2, 30);
                var score = (attendanceRate * 0.5) + progressScore + (avgQuality * 5);

                rankings.Add(new TeacherRankingDto
                {
                    Id = teacher.Id,
                    FullName = teacher.FullName,
                    StudentCount = studentIds.Count,
                    StudentAttendanceRate = Math.Round(attendanceRate, 1),
                    WeeklyProgress = teacherProgress.Count,
                    AverageQuality = Math.Round(avgQuality, 1),
                    Score = Math.Round(score, 1)
                });
            }

            return rankings.OrderByDescending(r => r.Score).Take(limit).ToList();
        }

        public async Task<List<AtRiskStudentDto>> GetAtRiskStudentsAsync(int limit = 20)
        {
            var today = DateTime.UtcNow.Date;
            var fromDate = today.AddDays(-7);

            var students = await _context.Students
                .Include(s => s.StudentHalaqat)
                    .ThenInclude(sh => sh.Halaqa)
                .Include(s => s.StudentHalaqat)
                    .ThenInclude(sh => sh.Teacher)
                .Where(s => s.StudentHalaqat.Any(sh => sh.IsActive))
                .ToListAsync();

            var atRiskStudents = new List<AtRiskStudentDto>();

            foreach (var student in students)
            {
                var activeAssignment = student.StudentHalaqat.FirstOrDefault(sh => sh.IsActive);
                if (activeAssignment == null) continue;

                var attendance = await _context.Attendances
                    .Where(a => a.StudentId == student.Id && a.Date >= fromDate && a.Date <= today)
                    .OrderByDescending(a => a.Date)
                    .ToListAsync();

                var lastProgress = await _context.ProgressRecords
                    .Where(p => p.StudentId == student.Id)
                    .OrderByDescending(p => p.Date)
                    .FirstOrDefaultAsync();

                var attendanceRate = attendance.Any()
                    ? (double)attendance.Count(a => a.Status == AttendanceStatus.Present) / attendance.Count * 100
                    : 0;

                var daysSinceProgress = lastProgress != null
                    ? (today - lastProgress.Date).Days
                    : 999;

                var consecutiveAbsences = 0;
                foreach (var a in attendance)
                {
                    if (a.Status == AttendanceStatus.Absent)
                        consecutiveAbsences++;
                    else
                        break;
                }

                if (attendanceRate < 70 || daysSinceProgress >= 7 || consecutiveAbsences >= 3)
                {
                    atRiskStudents.Add(new AtRiskStudentDto
                    {
                        Id = student.Id,
                        FullName = $"{student.FirstName} {student.LastName}",
                        HalaqaName = activeAssignment.Halaqa?.Name ?? "",
                        TeacherName = activeAssignment.Teacher?.FullName ?? "",
                        AttendanceRate = Math.Round(attendanceRate, 1),
                        DaysSinceLastProgress = daysSinceProgress,
                        ConsecutiveAbsences = consecutiveAbsences
                    });
                }
            }

            return atRiskStudents
                .OrderBy(s => s.AttendanceRate)
                .ThenByDescending(s => s.ConsecutiveAbsences)
                .ThenByDescending(s => s.DaysSinceLastProgress)
                .Take(limit)
                .ToList();
        }

        public async Task<List<AtRiskStudentDto>> GetTeacherAtRiskStudentsAsync(int teacherId, int limit = 10)
        {
            var today = DateTime.UtcNow.Date;
            var fromDate = today.AddDays(-7);

            var studentIds = await _context.StudentHalaqat
                .Where(sh => sh.TeacherId == teacherId && sh.IsActive)
                .Select(sh => sh.StudentId)
                .ToListAsync();

            var students = await _context.Students
                .Include(s => s.StudentHalaqat)
                    .ThenInclude(sh => sh.Halaqa)
                .Include(s => s.StudentHalaqat)
                    .ThenInclude(sh => sh.Teacher)
                .Where(s => studentIds.Contains(s.Id))
                .ToListAsync();

            var atRiskStudents = new List<AtRiskStudentDto>();

            foreach (var student in students)
            {
                var activeAssignment = student.StudentHalaqat.FirstOrDefault(sh => sh.IsActive && sh.TeacherId == teacherId);
                if (activeAssignment == null) continue;

                var attendance = await _context.Attendances
                    .Where(a => a.StudentId == student.Id && a.Date >= fromDate && a.Date <= today)
                    .OrderByDescending(a => a.Date)
                    .ToListAsync();

                var lastProgress = await _context.ProgressRecords
                    .Where(p => p.StudentId == student.Id)
                    .OrderByDescending(p => p.Date)
                    .FirstOrDefaultAsync();

                var attendanceRate = attendance.Any()
                    ? (double)attendance.Count(a => a.Status == AttendanceStatus.Present) / attendance.Count * 100
                    : 0;

                var daysSinceProgress = lastProgress != null
                    ? (today - lastProgress.Date).Days
                    : 999;

                var consecutiveAbsences = 0;
                foreach (var a in attendance)
                {
                    if (a.Status == AttendanceStatus.Absent)
                        consecutiveAbsences++;
                    else
                        break;
                }

                if (attendanceRate < 70 || daysSinceProgress >= 7 || consecutiveAbsences >= 3)
                {
                    atRiskStudents.Add(new AtRiskStudentDto
                    {
                        Id = student.Id,
                        FullName = $"{student.FirstName} {student.LastName}",
                        HalaqaName = activeAssignment.Halaqa?.Name ?? "",
                        TeacherName = activeAssignment.Teacher?.FullName ?? "",
                        AttendanceRate = Math.Round(attendanceRate, 1),
                        DaysSinceLastProgress = daysSinceProgress,
                        ConsecutiveAbsences = consecutiveAbsences
                    });
                }
            }

            return atRiskStudents
                .OrderBy(s => s.AttendanceRate)
                .ThenByDescending(s => s.ConsecutiveAbsences)
                .ThenByDescending(s => s.DaysSinceLastProgress)
                .Take(limit)
                .ToList();
        }
    }
}

