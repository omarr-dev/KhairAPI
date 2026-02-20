using Microsoft.EntityFrameworkCore;
using KhairAPI.Data;
using KhairAPI.Models.DTOs;
using KhairAPI.Models.Entities;
using KhairAPI.Services.Interfaces;
using KhairAPI.Core.Helpers;
using static KhairAPI.Core.Extensions.CacheKeys;
using static KhairAPI.Core.Extensions.CacheDurations;

namespace KhairAPI.Services.Implementations
{
    public class StatisticsService : IStatisticsService
    {
        private readonly AppDbContext _context;
        private readonly ICacheService _cache;

        public StatisticsService(AppDbContext context, ICacheService cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync(int? teacherId = null, List<int>? halaqaFilter = null)
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

                var studentIdsSet = studentIds.ToHashSet();
                studentQuery = studentQuery.Where(s => studentIds.Contains(s.Id));
                progressQuery = progressQuery.Where(p => p.TeacherId == teacherId);
                attendanceQuery = attendanceQuery.Where(a => studentIdsSet.Contains(a.StudentId));
            }
            else if (halaqaFilter != null)
            {
                var halaqaStudentIds = await _context.StudentHalaqat
                    .Where(sh => halaqaFilter.Contains(sh.HalaqaId) && sh.IsActive)
                    .Select(sh => sh.StudentId)
                    .ToListAsync();
                var halaqaStudentIdsSet = halaqaStudentIds.ToHashSet();
                studentQuery = studentQuery.Where(s => halaqaStudentIds.Contains(s.Id));
                progressQuery = progressQuery.Where(p => halaqaFilter.Contains(p.HalaqaId));
                attendanceQuery = attendanceQuery.Where(a => halaqaStudentIdsSet.Contains(a.StudentId));
            }

            // Execute queries sequentially (DbContext is not thread-safe)
            var totalStudents = await studentQuery.CountAsync();
            var totalTeachers = await _context.Teachers.CountAsync();

            // Combine Halaqat counts into a single query with projection
            var halaqatStats = await _context.Halaqat
                .GroupBy(h => 1)
                .Select(g => new { Total = g.Count(), Active = g.Count(h => h.IsActive) })
                .OrderBy(x => 1)
                .FirstOrDefaultAsync();

            var todayProgress = await progressQuery.ToListAsync();

            // Get both attendance counts in a single roundtrip using projection
            var attendanceStats = await attendanceQuery
                .GroupBy(a => 1)
                .Select(g => new
                {
                    Present = g.Count(a => a.Status == AttendanceStatus.Present),
                    Total = g.Count()
                })
                .OrderBy(x => 1)
                .FirstOrDefaultAsync();

            var totalHalaqat = halaqatStats?.Total ?? 0;
            var activeHalaqat = halaqatStats?.Active ?? 0;
            var todayAttendance = attendanceStats?.Present ?? 0;
            var totalTodayAttendance = attendanceStats?.Total ?? 0;

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

        public async Task<ReportStatsDto> GetReportStatsAsync(
            string dateRange,
            int? halaqaId = null,
            int? teacherId = null,
            List<int>? halaqaFilter = null,
            DateTime? customFromDate = null,
            DateTime? customToDate = null)
        {
            var today = DateTime.UtcNow.Date;

            // Determine date range based on option
            DateTime fromDate;
            DateTime toDate = today;

            if (dateRange == "custom" && customFromDate.HasValue && customToDate.HasValue)
            {
                // Use custom date range (already validated in controller)
                fromDate = customFromDate.Value.Date;
                toDate = customToDate.Value.Date;
            }
            else
            {
                fromDate = dateRange switch
                {
                    "week" => today.AddDays(-7),
                    "month" => today.AddMonths(-1),
                    _ => today.AddDays(-7) // Default to week if invalid option
                };
            }

            var progressQuery = _context.ProgressRecords.Where(p => p.Date >= fromDate && p.Date <= toDate);
            var attendanceQuery = _context.Attendances.Where(a => a.Date >= fromDate && a.Date <= toDate);
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
            else if (halaqaFilter != null)
            {
                progressQuery = progressQuery.Where(p => halaqaFilter.Contains(p.HalaqaId));
                attendanceQuery = attendanceQuery.Where(a => halaqaFilter.Contains(a.HalaqaId));
                var supervisorStudentIds = await _context.StudentHalaqat
                    .Where(sh => halaqaFilter.Contains(sh.HalaqaId) && sh.IsActive)
                    .Select(sh => sh.StudentId)
                    .ToListAsync();
                studentQuery = studentQuery.Where(s => supervisorStudentIds.Contains(s.Id));
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

            for (var date = fromDate; date <= toDate; date = date.AddDays(1))
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
            return await _cache.GetOrCreateAsync(
                SystemWideStats,
                async () => await GetSystemWideStatsInternalAsync(),
                Short);
        }

        private async Task<SystemWideStatsDto> GetSystemWideStatsInternalAsync()
        {
            var today = DateTime.UtcNow.Date;
            var yesterday = today.AddDays(-1);
            var weekStart = today.AddDays(-(int)today.DayOfWeek);

            // Get student count first
            var totalStudents = await _context.Students.CountAsync();

            // Load ALL progress records for the week in ONE query (includes today, yesterday, and week data)
            var weekProgress = await _context.ProgressRecords
                .Where(p => p.Date >= weekStart && p.Date <= today)
                .ToListAsync();

            // Filter today's progress in-memory (already loaded)
            var todayProgress = weekProgress.Where(p => p.Date == today).ToList();
            var todayVersesMemorized = todayProgress
                .Where(p => p.Type == ProgressType.Memorization)
                .Sum(p => Math.Max(0, p.ToVerse - p.FromVerse + 1));
            var todayVersesReviewed = todayProgress
                .Where(p => p.Type == ProgressType.Revision)
                .Sum(p => Math.Max(0, p.ToVerse - p.FromVerse + 1));
            var todayStudentsActive = todayProgress.Select(p => p.StudentId).Distinct().Count();

            // Filter yesterday's progress in-memory (already loaded)
            var yesterdayProgress = weekProgress.Where(p => p.Date == yesterday).ToList();
            var yesterdayVersesMemorized = yesterdayProgress
                .Where(p => p.Type == ProgressType.Memorization)
                .Sum(p => Math.Max(0, p.ToVerse - p.FromVerse + 1));
            var yesterdayVersesReviewed = yesterdayProgress
                .Where(p => p.Type == ProgressType.Revision)
                .Sum(p => Math.Max(0, p.ToVerse - p.FromVerse + 1));
            var yesterdayStudentsActive = yesterdayProgress.Select(p => p.StudentId).Distinct().Count();

            // Week stats use the full dataset
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

        public async Task<SupervisorDashboardDto> GetSupervisorDashboardAsync(List<int>? halaqaFilter = null)
        {
            var cacheKey = ForParams(SupervisorDashboard, halaqaFilter != null ? string.Join(",", halaqaFilter) : "all");

            return await _cache.GetOrCreateAsync(
                cacheKey,
                async () => await GetSupervisorDashboardInternalAsync(halaqaFilter),
                Short,
                size: 5);
        }

        private async Task<SupervisorDashboardDto> GetSupervisorDashboardInternalAsync(List<int>? halaqaFilter = null)
        {
            var today = DateTime.UtcNow.Date;
            var weekAgo = today.AddDays(-7);

            // Execute queries sequentially (DbContext is not thread-safe)
            var totalStudents = await _context.Students.CountAsync();
            var totalTeachers = await _context.Teachers.CountAsync();
            var totalHalaqat = await _context.Halaqat.Where(h => h.IsActive).CountAsync();
            var todayAttendance = await _context.Attendances.Where(a => a.Date == today).ToListAsync();
            var todayProgress = await _context.ProgressRecords.Where(p => p.Date == today).ToListAsync();

            var todayAttendanceRate = todayAttendance.Any()
                ? (double)todayAttendance.Count(a => a.Status == AttendanceStatus.Present) / todayAttendance.Count * 100
                : 0;

            var todayMemorization = todayProgress.Count(p => p.Type == ProgressType.Memorization);
            var todayRevision = todayProgress.Count(p => p.Type == ProgressType.Revision);

            // Execute ranking methods sequentially (they share the same DbContext)
            var halaqatStats = await GetHalaqaRankingInternalAsync(7, 5, halaqaFilter);
            var teacherStats = await GetTeacherRankingInternalAsync(7, 5, halaqaFilter);
            var atRiskStudents = await GetAtRiskStudentsInternalAsync(10, halaqaFilter);

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

        public async Task<List<HalaqaRankingDto>> GetHalaqaRankingAsync(int days = 7, int limit = 10, List<int>? halaqaFilter = null)
        {
            var cacheKey = ForParams(HalaqaRanking, days, limit);

            return await _cache.GetOrCreateAsync(
                cacheKey,
                async () => await GetHalaqaRankingInternalAsync(days, limit, halaqaFilter),
                Medium,
                size: 3);
        }

        private async Task<List<HalaqaRankingDto>> GetHalaqaRankingInternalAsync(int days = 7, int limit = 10, List<int>? halaqaFilter = null)
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

        public async Task<List<TeacherRankingDto>> GetTeacherRankingAsync(int days = 7, int limit = 10, List<int>? halaqaFilter = null)
        {
            var cacheKey = ForParams(TeacherRanking, days, limit);

            return await _cache.GetOrCreateAsync(
                cacheKey,
                async () => await GetTeacherRankingInternalAsync(days, limit, halaqaFilter),
                Medium,
                size: 3);
        }

        private async Task<List<TeacherRankingDto>> GetTeacherRankingInternalAsync(int days = 7, int limit = 10, List<int>? halaqaFilter = null)
        {
            var today = DateTime.UtcNow.Date;
            var fromDate = today.AddDays(-days);

            // Load all teachers with their student relationships
            var teachers = await _context.Teachers
                .Include(t => t.StudentHalaqat)
                .ToListAsync();

            // Batch load ALL attendance records for the date range (eliminates N queries)
            var allAttendances = await _context.Attendances
                .Where(a => a.Date >= fromDate && a.Date <= today)
                .ToListAsync();

            // Batch load ALL progress records for the date range (eliminates N queries)
            var allProgressRecords = await _context.ProgressRecords
                .Where(p => p.Date >= fromDate && p.Date <= today)
                .ToListAsync();

            var rankings = new List<TeacherRankingDto>();

            foreach (var teacher in teachers)
            {
                var studentIds = teacher.StudentHalaqat
                    .Where(sh => sh.IsActive)
                    .Select(sh => sh.StudentId)
                    .ToHashSet(); // Use HashSet for O(1) lookups

                if (!studentIds.Any()) continue;

                // Filter in-memory (fast O(n) operation)
                var studentAttendance = allAttendances
                    .Where(a => studentIds.Contains(a.StudentId))
                    .ToList();

                var teacherProgress = allProgressRecords
                    .Where(p => p.TeacherId == teacher.Id)
                    .ToList();

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

        public async Task<List<AtRiskStudentDto>> GetAtRiskStudentsAsync(int limit = 20, List<int>? halaqaFilter = null)
        {
            return await GetAtRiskStudentsInternalAsync(limit, halaqaFilter);
        }

        private async Task<List<AtRiskStudentDto>> GetAtRiskStudentsInternalAsync(int limit = 20, List<int>? halaqaFilter = null)
        {
            var today = DateTime.UtcNow.Date;
            var fromDate = today.AddDays(-30);

            // Load all students with their halaqa assignments
            var studentsQuery = _context.Students
                .Include(s => s.StudentHalaqat)
                    .ThenInclude(sh => sh.Halaqa)
                .Include(s => s.StudentHalaqat)
                    .ThenInclude(sh => sh.Teacher)
                .Where(s => s.StudentHalaqat.Any(sh => sh.IsActive));

            if (halaqaFilter != null)
            {
                studentsQuery = studentsQuery
                    .Where(s => s.StudentHalaqat.Any(sh => sh.IsActive && halaqaFilter.Contains(sh.HalaqaId)));
            }

            var students = await studentsQuery.ToListAsync();

            var studentIds = students.Select(s => s.Id).ToHashSet();

            // Batch load ALL attendance for these students in the date range (eliminates N queries)
            var allAttendances = await _context.Attendances
                .Where(a => studentIds.Contains(a.StudentId) && a.Date >= fromDate && a.Date <= today)
                .ToListAsync();

            // Batch load the LATEST progress record for each student (eliminates N queries)
            // We get all progress records and then find the latest per student in-memory
            var allProgressRecords = await _context.ProgressRecords
                .Where(p => studentIds.Contains(p.StudentId))
                .ToListAsync();

            // Group progress by student and get the latest for each
            var latestProgressByStudent = allProgressRecords
                .GroupBy(p => p.StudentId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.Date).FirstOrDefault());

            // Group attendance by student for quick lookup
            var attendanceByStudent = allAttendances
                .GroupBy(a => a.StudentId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.Date).ToList());

            var atRiskStudents = new List<AtRiskStudentDto>();

            foreach (var student in students)
            {
                var activeAssignment = student.StudentHalaqat.FirstOrDefault(sh => sh.IsActive);
                if (activeAssignment == null) continue;

                // Get attendance from in-memory dictionary (O(1) lookup)
                var studentAttendance = attendanceByStudent.TryGetValue(student.Id, out var att) ? att : new List<Attendance>();

                // Get latest progress from in-memory dictionary (O(1) lookup)
                var lastProgress = latestProgressByStudent.TryGetValue(student.Id, out var prog) ? prog : null;

                // For new students without progress, use their creation date instead of 999
                var daysSinceProgress = lastProgress != null
                    ? (today - lastProgress.Date).Days
                    : (today - student.CreatedAt.Date).Days;

                // Simple logic: Count consecutive absences on scheduled days only (ignore missing records)
                var activeDays = ParseActiveDays(activeAssignment.Halaqa?.ActiveDays);
                var enrollmentDate = activeAssignment.EnrollmentDate.Date;
                var consecutiveAbsences = 0;
                var currentCheckDate = today;

                // Go backwards through scheduled days and count consecutive absences
                while ((today - currentCheckDate).Days < 30 && currentCheckDate >= enrollmentDate)
                {
                    if (activeDays.Contains((int)currentCheckDate.DayOfWeek))
                    {
                        var record = studentAttendance.FirstOrDefault(a => a.Date.Date == currentCheckDate.Date);

                        // Only count explicit Absent records (ignore missing records)
                        if (record != null && record.Status == AttendanceStatus.Absent)
                        {
                            consecutiveAbsences++;
                        }
                        else if (record != null && record.Status == AttendanceStatus.Present)
                        {
                            // Stop counting when we hit a present record
                            break;
                        }
                        // If no record (missing), ignore and continue checking older dates
                    }
                    currentCheckDate = currentCheckDate.AddDays(-1);
                }

                // Flag as at-risk if 3 or more consecutive absences
                if (consecutiveAbsences >= 3)
                {
                    atRiskStudents.Add(new AtRiskStudentDto
                    {
                        Id = student.Id,
                        FullName = $"{student.FirstName} {student.LastName}",
                        HalaqaName = activeAssignment.Halaqa?.Name ?? "",
                        TeacherName = activeAssignment.Teacher?.FullName ?? "",
                        AttendanceRate = 0, // Not used anymore, will show consecutive absences instead
                        DaysSinceLastProgress = daysSinceProgress,
                        ConsecutiveAbsences = consecutiveAbsences
                    });
                }
            }

            return atRiskStudents
                .OrderByDescending(s => s.ConsecutiveAbsences)
                .ThenBy(s => s.AttendanceRate)
                .Take(limit)
                .ToList();
        }

        public async Task<List<AtRiskStudentDto>> GetTeacherAtRiskStudentsAsync(int teacherId, int limit = 10)
        {
            var today = DateTime.UtcNow.Date;
            var fromDate = today.AddDays(-30);

            // Get student IDs for this teacher (use HashSet for O(1) lookups)
            var studentIdsList = await _context.StudentHalaqat
                .Where(sh => sh.TeacherId == teacherId && sh.IsActive)
                .Select(sh => sh.StudentId)
                .ToListAsync();

            var studentIdsSet = studentIdsList.ToHashSet();

            // Load students with their halaqa assignments
            var students = await _context.Students
                .Include(s => s.StudentHalaqat)
                    .ThenInclude(sh => sh.Halaqa)
                .Include(s => s.StudentHalaqat)
                    .ThenInclude(sh => sh.Teacher)
                .Where(s => studentIdsList.Contains(s.Id))
                .ToListAsync();

            // Batch load ALL attendance for these students in the date range (eliminates N queries)
            var allAttendances = await _context.Attendances
                .Where(a => studentIdsSet.Contains(a.StudentId) && a.Date >= fromDate && a.Date <= today)
                .ToListAsync();

            // Batch load ALL progress records for these students (eliminates N queries)
            var allProgressRecords = await _context.ProgressRecords
                .Where(p => studentIdsSet.Contains(p.StudentId))
                .ToListAsync();

            // Group progress by student and get the latest for each
            var latestProgressByStudent = allProgressRecords
                .GroupBy(p => p.StudentId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.Date).FirstOrDefault());

            // Group attendance by student for quick lookup
            var attendanceByStudent = allAttendances
                .GroupBy(a => a.StudentId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.Date).ToList());

            var atRiskStudents = new List<AtRiskStudentDto>();

            foreach (var student in students)
            {
                var activeAssignment = student.StudentHalaqat.FirstOrDefault(sh => sh.IsActive && sh.TeacherId == teacherId);
                if (activeAssignment == null) continue;

                // Get attendance from in-memory dictionary (O(1) lookup)
                var studentAttendance = attendanceByStudent.TryGetValue(student.Id, out var att) ? att : new List<Attendance>();

                // Get latest progress from in-memory dictionary (O(1) lookup)
                var lastProgress = latestProgressByStudent.TryGetValue(student.Id, out var prog) ? prog : null;

                // For new students without progress, use their creation date instead of 999
                var daysSinceProgress = lastProgress != null
                    ? (today - lastProgress.Date).Days
                    : (today - student.CreatedAt.Date).Days;

                // Simple logic: Count consecutive absences on scheduled days only (ignore missing records)
                var activeDays = ParseActiveDays(activeAssignment.Halaqa?.ActiveDays);
                var enrollmentDate = activeAssignment.EnrollmentDate.Date;
                var consecutiveAbsences = 0;
                var currentCheckDate = today;

                // Go backwards through scheduled days and count consecutive absences
                while ((today - currentCheckDate).Days < 30 && currentCheckDate >= enrollmentDate)
                {
                    if (activeDays.Contains((int)currentCheckDate.DayOfWeek))
                    {
                        var record = studentAttendance.FirstOrDefault(a => a.Date.Date == currentCheckDate.Date);

                        // Only count explicit Absent records (ignore missing records)
                        if (record != null && record.Status == AttendanceStatus.Absent)
                        {
                            consecutiveAbsences++;
                        }
                        else if (record != null && record.Status == AttendanceStatus.Present)
                        {
                            // Stop counting when we hit a present record
                            break;
                        }
                        // If no record (missing), ignore and continue checking older dates
                    }
                    currentCheckDate = currentCheckDate.AddDays(-1);
                }

                // Flag as at-risk if 3 or more consecutive absences
                if (consecutiveAbsences >= 3)
                {
                    atRiskStudents.Add(new AtRiskStudentDto
                    {
                        Id = student.Id,
                        FullName = $"{student.FirstName} {student.LastName}",
                        HalaqaName = activeAssignment.Halaqa?.Name ?? "",
                        TeacherName = activeAssignment.Teacher?.FullName ?? "",
                        AttendanceRate = 0, // Not used anymore, will show consecutive absences instead
                        DaysSinceLastProgress = daysSinceProgress,
                        ConsecutiveAbsences = consecutiveAbsences
                    });
                }
            }

            return atRiskStudents
                .OrderByDescending(s => s.ConsecutiveAbsences)
                .ThenBy(s => s.AttendanceRate)
                .Take(limit)
                .ToList();
        }

        private static List<int> ParseActiveDays(string? activeDays)
        {
            if (string.IsNullOrWhiteSpace(activeDays))
                return new List<int> { 0, 1, 2, 3, 4 }; // Default to Sun-Thu if not set

            return activeDays
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(d => int.TryParse(d.Trim(), out var day) ? day : -1)
                .Where(d => d >= 0 && d <= 6)
                .ToList();
        }

        /// <summary>
        /// Gets the streak leaderboard showing students with longest consecutive target achievement days.
        /// Uses stored streak values from StudentTarget for performance (updated via ProgressService and background job).
        /// Students without targets will have 0 streak.
        /// </summary>
        public async Task<StreakLeaderboardDto> GetStreakLeaderboardAsync(StreakLeaderboardFilterDto filter)
        {
            var today = DateTime.UtcNow.Date;

            // Build base query for students based on access scope
            IQueryable<StudentHalaqa> studentHalaqaQuery = _context.StudentHalaqat
                .Where(sh => sh.IsActive);

            // Apply filters based on access level
            if (filter.TeacherId.HasValue)
            {
                // Teacher sees only their students
                studentHalaqaQuery = studentHalaqaQuery.Where(sh => sh.TeacherId == filter.TeacherId);
            }
            else if (filter.SupervisedHalaqaIds != null && filter.SupervisedHalaqaIds.Any())
            {
                // HalaqaSupervisor sees only their assigned halaqat
                studentHalaqaQuery = studentHalaqaQuery.Where(sh => filter.SupervisedHalaqaIds.Contains(sh.HalaqaId));
            }
            // Full Supervisor sees all (no additional filter)

            // Further filter by selected halaqa if provided
            if (filter.SelectedHalaqaId.HasValue)
            {
                studentHalaqaQuery = studentHalaqaQuery.Where(sh => sh.HalaqaId == filter.SelectedHalaqaId);
            }

            // Further filter by selected teacher if provided (for supervisors)
            if (filter.SelectedTeacherId.HasValue)
            {
                studentHalaqaQuery = studentHalaqaQuery.Where(sh => sh.TeacherId == filter.SelectedTeacherId);
            }

            // Get student info with their halaqa details (single query with projection)
            var studentAssignments = await studentHalaqaQuery
                .Select(sh => new
                {
                    sh.StudentId,
                    sh.HalaqaId,
                    StudentFirstName = sh.Student!.FirstName,
                    StudentLastName = sh.Student.LastName,
                    HalaqaName = sh.Halaqa!.Name,
                    HalaqaActiveDays = sh.Halaqa.ActiveDays
                })
                .ToListAsync();

            // Use the first active assignment per student (students may have multiple)
            var studentLookup = studentAssignments
                .GroupBy(s => s.StudentId)
                .ToDictionary(g => g.Key, g => g.First());

            var studentIds = studentLookup.Keys.ToHashSet();
            var totalStudentsInScope = studentIds.Count;

            if (totalStudentsInScope == 0)
            {
                return new StreakLeaderboardDto
                {
                    TotalStudentsInScope = 0,
                    StudentsWithActiveStreaks = 0
                };
            }

            // Get halaqa name if filtering by specific halaqa
            string? filteredHalaqaName = null;
            if (filter.SelectedHalaqaId.HasValue && studentAssignments.Any())
            {
                filteredHalaqaName = studentAssignments.First().HalaqaName;
            }

            // Get student targets with stored streak values (single optimized query)
            var studentTargets = await _context.StudentTargets
                .AsNoTracking()
                .Where(t => studentIds.Contains(t.StudentId))
                .Select(t => new
                {
                    t.StudentId,
                    t.CurrentStreak,
                    t.LongestStreak,
                    t.LastStreakDate
                })
                .ToListAsync();

            var targetsByStudent = studentTargets.ToDictionary(t => t.StudentId);

            // Pre-calculate active days for each halaqa to determine if streak is active
            var activeDaysByHalaqa = studentAssignments
                .GroupBy(s => s.HalaqaActiveDays)
                .ToDictionary(
                    g => g.Key ?? string.Empty,
                    g => ParseActiveDays(g.Key).ToHashSet()
                );

            // Build streak DTOs for each student
            var studentStreaks = new List<StudentStreakDto>(studentIds.Count);

            foreach (var studentId in studentIds)
            {
                var assignment = studentLookup[studentId];

                // Check if student has targets with streak data
                if (!targetsByStudent.TryGetValue(studentId, out var target))
                {
                    // No target, no streak
                    studentStreaks.Add(new StudentStreakDto
                    {
                        StudentId = studentId,
                        StudentName = $"{assignment.StudentFirstName} {assignment.StudentLastName}",
                        HalaqaId = assignment.HalaqaId,
                        HalaqaName = assignment.HalaqaName,
                        CurrentStreak = 0,
                        LongestStreak = 0,
                        IsStreakActive = false,
                        LastProgressDate = null
                    });
                    continue;
                }

                // Get cached active days for this halaqa
                var activeDaysSet = activeDaysByHalaqa[assignment.HalaqaActiveDays ?? string.Empty];

                // Determine if streak is active:
                // - If LastStreakDate is today, streak is active
                // - If today is not an active day and LastStreakDate was the most recent active day, streak is active
                bool isActive = false;
                if (target.LastStreakDate.HasValue && target.CurrentStreak > 0)
                {
                    if (target.LastStreakDate.Value.Date == today)
                    {
                        isActive = true;
                    }
                    else if (!activeDaysSet.Contains((int)today.DayOfWeek))
                    {
                        // Today is not an active day, find the most recent active day
                        var checkDate = today.AddDays(-1);
                        while ((today - checkDate).Days <= 7)
                        {
                            if (activeDaysSet.Contains((int)checkDate.DayOfWeek))
                            {
                                isActive = (target.LastStreakDate.Value.Date == checkDate);
                                break;
                            }
                            checkDate = checkDate.AddDays(-1);
                        }
                    }
                }

                studentStreaks.Add(new StudentStreakDto
                {
                    StudentId = studentId,
                    StudentName = $"{assignment.StudentFirstName} {assignment.StudentLastName}",
                    HalaqaId = assignment.HalaqaId,
                    HalaqaName = assignment.HalaqaName,
                    CurrentStreak = target.CurrentStreak,
                    LongestStreak = target.LongestStreak,
                    IsStreakActive = isActive,
                    LastProgressDate = target.LastStreakDate
                });
            }

            // Sort and rank efficiently - only sort students with streaks > 0 for leaderboard
            var studentsWithStreaks = studentStreaks.Where(s => s.CurrentStreak > 0).ToList();

            var rankedStudents = studentsWithStreaks
                .OrderByDescending(s => s.CurrentStreak)
                .ThenByDescending(s => s.LongestStreak)
                .ThenBy(s => s.StudentName, StringComparer.Ordinal)
                .Take(filter.Limit)
                .Select((s, index) => { s.Rank = index + 1; return s; })
                .ToList();

            return new StreakLeaderboardDto
            {
                Students = rankedStudents,
                TotalStudentsInScope = totalStudentsInScope,
                StudentsWithActiveStreaks = studentsWithStreaks.Count,
                FilteredByHalaqa = filteredHalaqaName
            };
        }

        public async Task<TargetAdoptionOverviewDto> GetTargetAdoptionOverviewAsync(TargetAdoptionFilterDto filter)
        {
            var today = DateTime.UtcNow.Date;
            var oneWeekAgo = today.AddDays(-7);

            // Build base query for students based on access scope
            IQueryable<StudentHalaqa> studentHalaqaQuery = _context.StudentHalaqat
                .Where(sh => sh.IsActive);

            // Apply filters based on access level
            if (filter.TeacherId.HasValue)
            {
                // Teacher sees only their students
                studentHalaqaQuery = studentHalaqaQuery.Where(sh => sh.TeacherId == filter.TeacherId);
            }
            else if (filter.SupervisedHalaqaIds != null && filter.SupervisedHalaqaIds.Any())
            {
                // HalaqaSupervisor sees only their assigned halaqat
                studentHalaqaQuery = studentHalaqaQuery.Where(sh => filter.SupervisedHalaqaIds.Contains(sh.HalaqaId));
            }
            // Full Supervisor sees all (no additional filter)

            // Further filter by selected halaqa if provided
            if (filter.SelectedHalaqaId.HasValue)
            {
                studentHalaqaQuery = studentHalaqaQuery.Where(sh => sh.HalaqaId == filter.SelectedHalaqaId);
            }

            // Further filter by selected teacher if provided (for supervisors)
            if (filter.SelectedTeacherId.HasValue)
            {
                studentHalaqaQuery = studentHalaqaQuery.Where(sh => sh.TeacherId == filter.SelectedTeacherId);
            }

            // Get student IDs in scope with their halaqa and teacher info (single query)
            var studentAssignments = await studentHalaqaQuery
                .Select(sh => new
                {
                    sh.StudentId,
                    sh.HalaqaId,
                    sh.TeacherId,
                    HalaqaName = sh.Halaqa!.Name
                })
                .ToListAsync();

            var studentIds = studentAssignments.Select(s => s.StudentId).Distinct().ToHashSet();
            var halaqaIds = studentAssignments.Select(s => s.HalaqaId).Distinct().ToHashSet();
            var teacherIds = studentAssignments.Select(s => s.TeacherId).Distinct().ToHashSet();

            var totalStudents = studentIds.Count;

            if (totalStudents == 0)
            {
                return new TargetAdoptionOverviewDto
                {
                    CoveragePercentage = 0,
                    StudentsWithTargets = 0,
                    TotalStudents = 0,
                    WeeklyChangePercentage = 0,
                    HalaqaCoverage = new HalaqaCoverageDto { HalaqatWithTargets = 0, TotalHalaqat = 0 },
                    TeacherCoverage = new TeacherCoverageDto { TeachersWithTargets = 0, TotalTeachers = 0 },
                    ActivationRate = 0,
                    HalaqaBreakdown = new List<HalaqaTargetStatsDto>()
                };
            }

            // Get all targets for students in scope (single query)
            var studentTargets = await _context.StudentTargets
                .Where(t => studentIds.Contains(t.StudentId))
                .Select(t => new
                {
                    t.StudentId,
                    t.CreatedAt,
                    HasMemorizationTarget = t.MemorizationLinesTarget.HasValue,
                    HasRevisionTarget = t.RevisionPagesTarget.HasValue,
                    HasConsolidationTarget = t.ConsolidationPagesTarget.HasValue
                })
                .ToListAsync();

            // Calculate current coverage
            // Note: StudentTarget has unique constraint on StudentId, so Count() = distinct students
            var studentIdsWithTargets = studentTargets.Select(t => t.StudentId).ToHashSet();
            var studentsWithTargets = studentIdsWithTargets.Count;
            var coveragePercentage = totalStudents > 0
                ? Math.Round((double)studentsWithTargets / totalStudents * 100, 1)
                : 0;

            // Calculate weekly change - students with targets created before one week ago vs now
            var studentsWithTargetsLastWeek = studentTargets
                .Where(t => t.CreatedAt <= oneWeekAgo)
                .Select(t => t.StudentId)
                .Distinct()
                .Count();
            var totalStudentsLastWeek = totalStudents; // Approximation - actual would need historical data

            var lastWeekPercentage = totalStudentsLastWeek > 0
                ? (double)studentsWithTargetsLastWeek / totalStudentsLastWeek * 100
                : 0;
            var weeklyChangePercentage = Math.Round(coveragePercentage - lastWeekPercentage, 1);

            // Calculate halaqa coverage - halaqat with at least one student having targets
            // Use HashSet for O(1) lookup instead of LINQ Where + Contains
            var halaqatWithTargets = studentAssignments
                .Where(sa => studentIdsWithTargets.Contains(sa.StudentId))
                .Select(sa => sa.HalaqaId)
                .Distinct()
                .Count();

            // Calculate teacher coverage - teachers with at least one student having targets
            var teachersWithTargets = studentAssignments
                .Where(sa => studentIdsWithTargets.Contains(sa.StudentId))
                .Select(sa => sa.TeacherId)
                .Distinct()
                .Count();

            // Calculate activation rate - percentage of students with targets who have recent progress (last 7 days)
            var recentProgressStudentIds = await _context.ProgressRecords
                .Where(p => studentIdsWithTargets.Contains(p.StudentId) && p.Date >= oneWeekAgo && p.Date <= today)
                .Select(p => p.StudentId)
                .Distinct()
                .ToListAsync();

            var activationRate = studentsWithTargets > 0
                ? Math.Round((double)recentProgressStudentIds.Count / studentsWithTargets * 100, 1)
                : 0;

            var result = new TargetAdoptionOverviewDto
            {
                CoveragePercentage = coveragePercentage,
                StudentsWithTargets = studentsWithTargets,
                TotalStudents = totalStudents,
                WeeklyChangePercentage = weeklyChangePercentage,
                HalaqaCoverage = new HalaqaCoverageDto
                {
                    HalaqatWithTargets = halaqatWithTargets,
                    TotalHalaqat = halaqaIds.Count
                },
                TeacherCoverage = new TeacherCoverageDto
                {
                    TeachersWithTargets = teachersWithTargets,
                    TotalTeachers = teacherIds.Count
                },
                ActivationRate = activationRate
            };

            // Include per-halaqa breakdown if requested
            if (filter.IncludeHalaqaBreakdown)
            {
                // Group by halaqa and calculate stats efficiently
                var halaqaGroups = studentAssignments
                    .GroupBy(sa => new { sa.HalaqaId, sa.HalaqaName })
                    .Select(g =>
                    {
                        // Use HashSet for O(1) lookups instead of repeated Contains() calls
                        var halaqaStudentIds = g.Select(x => x.StudentId).ToHashSet();
                        var totalStudents = halaqaStudentIds.Count;
                        var studentsWithTargets = halaqaStudentIds.Count(id => studentIdsWithTargets.Contains(id));

                        return new HalaqaTargetStatsDto
                        {
                            HalaqaId = g.Key.HalaqaId,
                            HalaqaName = g.Key.HalaqaName,
                            TotalStudents = totalStudents,
                            StudentsWithTargets = studentsWithTargets,
                            CoveragePercentage = totalStudents > 0
                                ? Math.Round((double)studentsWithTargets / totalStudents * 100, 1)
                                : 0
                        };
                    })
                    .OrderByDescending(h => h.CoveragePercentage)
                    .ThenByDescending(h => h.TotalStudents)
                    .ToList();

                result.HalaqaBreakdown = halaqaGroups;
            }

            return result;
        }

        /// <summary>
        /// Gets daily achievement statistics showing aggregated progress vs targets.
        /// إنجاز اليوم - إحصائيات الإنجاز اليومي المجمّعة
        /// </summary>
        public async Task<DailyAchievementStatsDto> GetDailyAchievementStatsAsync(DailyAchievementFilterDto filter)
        {
            var fromDate = filter.FromDate.Date;
            var toDate = filter.ToDate.Date;
            var nextDayAfterEnd = toDate.AddDays(1);

            // Build base query for students based on access scope
            IQueryable<StudentHalaqa> studentHalaqaQuery = _context.StudentHalaqat
                .Where(sh => sh.IsActive);

            // Apply filters based on access level
            if (filter.TeacherId.HasValue)
            {
                // Teacher sees only their students
                studentHalaqaQuery = studentHalaqaQuery.Where(sh => sh.TeacherId == filter.TeacherId);
            }
            else if (filter.SupervisedHalaqaIds != null && filter.SupervisedHalaqaIds.Any())
            {
                // HalaqaSupervisor sees only their assigned halaqat
                studentHalaqaQuery = studentHalaqaQuery.Where(sh => filter.SupervisedHalaqaIds.Contains(sh.HalaqaId));
            }
            // Full Supervisor sees all (no additional filter)

            // Further filter by selected halaqa if provided
            if (filter.SelectedHalaqaId.HasValue)
            {
                studentHalaqaQuery = studentHalaqaQuery.Where(sh => sh.HalaqaId == filter.SelectedHalaqaId);
            }

            // Further filter by selected teacher if provided (for supervisors)
            if (filter.SelectedTeacherId.HasValue)
            {
                studentHalaqaQuery = studentHalaqaQuery.Where(sh => sh.TeacherId == filter.SelectedTeacherId);
            }

            // Get students with their halaqa active days (needed for accurate target calculation)
            var studentAssignments = await studentHalaqaQuery
                .Select(sh => new
                {
                    sh.StudentId,
                    HalaqaActiveDays = sh.Halaqa!.ActiveDays
                })
                .ToListAsync();

            // Use first assignment per student (students may have multiple halaqat)
            var studentHalaqaLookup = studentAssignments
                .GroupBy(s => s.StudentId)
                .ToDictionary(g => g.Key, g => g.First().HalaqaActiveDays);

            var studentIds = studentHalaqaLookup.Keys.ToList();
            var studentIdsSet = studentIds.ToHashSet();
            var totalStudents = studentIds.Count;

            if (totalStudents == 0)
            {
                return new DailyAchievementStatsDto
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    TotalStudents = 0,
                    StudentsWithTargets = 0,
                    Memorization = new AchievementCategoryDto { Unit = "سطر" },
                    Revision = new AchievementCategoryDto { Unit = "وجه" },
                    Consolidation = new AchievementCategoryDto { Unit = "وجه" },
                    WeekSummary = new WeekSummaryDto { TotalDays = (int)(toDate - fromDate).TotalDays + 1 }
                };
            }

            // Get all targets for students in scope (single query)
            var targets = await _context.StudentTargets
                .AsNoTracking()
                .Where(t => studentIdsSet.Contains(t.StudentId))
                .Select(t => new
                {
                    t.StudentId,
                    t.MemorizationLinesTarget,
                    t.RevisionPagesTarget,
                    t.ConsolidationPagesTarget
                })
                .ToListAsync();

            var studentsWithTargets = targets.Count;

            // Calculate number of days in the range (inclusive)
            var numberOfDays = (int)(toDate - fromDate).TotalDays + 1;

            // Helper function to count active days in date range for a given halaqa schedule
            int CountActiveDaysInRange(string? activeDaysStr)
            {
                var activeDays = ParseActiveDays(activeDaysStr).ToHashSet();
                int count = 0;
                for (var date = fromDate; date <= toDate; date = date.AddDays(1))
                {
                    if (activeDays.Contains((int)date.DayOfWeek))
                        count++;
                }
                return count;
            }

            // Calculate cumulative targets based on each student's halaqa active days
            // Target = sum of (student's daily target × their halaqa's active days in range)
            int totalMemorizationTarget = 0;
            int totalRevisionTarget = 0;
            int totalConsolidationTarget = 0;

            // Also calculate daily targets (for the daily percentage calculation)
            var dailyMemorizationTarget = targets.Sum(t => t.MemorizationLinesTarget ?? 0);
            var dailyRevisionTarget = targets.Sum(t => t.RevisionPagesTarget ?? 0);
            var dailyConsolidationTarget = targets.Sum(t => t.ConsolidationPagesTarget ?? 0);

            // Cache active days count per unique schedule to avoid recalculating
            var activeDaysCountCache = new Dictionary<string, int>();

            foreach (var target in targets)
            {
                var halaqaActiveDays = studentHalaqaLookup.TryGetValue(target.StudentId, out var days) ? days : null;
                var cacheKey = halaqaActiveDays ?? "";

                if (!activeDaysCountCache.TryGetValue(cacheKey, out int activeDaysCount))
                {
                    activeDaysCount = CountActiveDaysInRange(halaqaActiveDays);
                    activeDaysCountCache[cacheKey] = activeDaysCount;
                }

                totalMemorizationTarget += (target.MemorizationLinesTarget ?? 0) * activeDaysCount;
                totalRevisionTarget += (target.RevisionPagesTarget ?? 0) * activeDaysCount;
                totalConsolidationTarget += (target.ConsolidationPagesTarget ?? 0) * activeDaysCount;
            }

            // Get progress records grouped by date and type (optimized single query)
            var progressAggregations = await _context.ProgressRecords
                .AsNoTracking()
                .Where(p => studentIdsSet.Contains(p.StudentId) && p.Date >= fromDate && p.Date < nextDayAfterEnd)
                .GroupBy(p => new { p.Date.Date, p.Type })
                .Select(g => new
                {
                    Date = g.Key.Date,
                    Type = g.Key.Type,
                    TotalLines = g.Sum(p => p.NumberLines)
                })
                .ToListAsync();

            // Calculate cumulative achievements across ALL days in the range
            const double LinesPerPage = 15.0;

            var memorizationAchieved = progressAggregations
                .Where(p => p.Type == ProgressType.Memorization)
                .Sum(p => p.TotalLines);

            var revisionLinesAchieved = progressAggregations
                .Where(p => p.Type == ProgressType.Revision)
                .Sum(p => p.TotalLines);
            var revisionPagesAchieved = revisionLinesAchieved / LinesPerPage;

            var consolidationLinesAchieved = progressAggregations
                .Where(p => p.Type == ProgressType.Consolidation)
                .Sum(p => p.TotalLines);
            var consolidationPagesAchieved = consolidationLinesAchieved / LinesPerPage;

            // Build week summary (daily target achievement status)
            var daySummaries = new List<DayAchievementStatusDto>();

            for (var date = fromDate; date <= toDate; date = date.AddDays(1))
            {
                var dayProgress = progressAggregations.Where(p => p.Date == date).ToList();

                var dayMemLines = dayProgress
                    .Where(p => p.Type == ProgressType.Memorization)
                    .Sum(p => p.TotalLines);
                var dayRevLines = dayProgress
                    .Where(p => p.Type == ProgressType.Revision)
                    .Sum(p => p.TotalLines);
                var dayConLines = dayProgress
                    .Where(p => p.Type == ProgressType.Consolidation)
                    .Sum(p => p.TotalLines);

                var dayRevPages = dayRevLines / LinesPerPage;
                var dayConPages = dayConLines / LinesPerPage;

                // Calculate day's overall percentage (using daily targets, not cumulative)
                double dayPercentage = 0;
                int categoryCount = 0;

                if (dailyMemorizationTarget > 0)
                {
                    dayPercentage += Math.Min(100, dayMemLines / dailyMemorizationTarget * 100);
                    categoryCount++;
                }
                if (dailyRevisionTarget > 0)
                {
                    dayPercentage += Math.Min(100, dayRevPages / dailyRevisionTarget * 100);
                    categoryCount++;
                }
                if (dailyConsolidationTarget > 0)
                {
                    dayPercentage += Math.Min(100, dayConPages / dailyConsolidationTarget * 100);
                    categoryCount++;
                }

                var avgPercentage = categoryCount > 0 ? dayPercentage / categoryCount : 0;

                // Target met if all set daily targets are >= 100% for this day
                var targetMet =
                    (dailyMemorizationTarget == 0 || dayMemLines >= dailyMemorizationTarget) &&
                    (dailyRevisionTarget == 0 || dayRevPages >= dailyRevisionTarget) &&
                    (dailyConsolidationTarget == 0 || dayConPages >= dailyConsolidationTarget) &&
                    categoryCount > 0; // At least one target must be set

                daySummaries.Add(new DayAchievementStatusDto
                {
                    Date = date,
                    TargetMet = targetMet,
                    Percentage = Math.Round(avgPercentage, 1)
                });
            }

            // Calculate percentages
            var memPercentage = totalMemorizationTarget > 0
                ? Math.Min(100, Math.Round(memorizationAchieved / totalMemorizationTarget * 100, 1))
                : 0;
            var revPercentage = totalRevisionTarget > 0
                ? Math.Min(100, Math.Round(revisionPagesAchieved / totalRevisionTarget * 100, 1))
                : 0;
            var conPercentage = totalConsolidationTarget > 0
                ? Math.Min(100, Math.Round(consolidationPagesAchieved / totalConsolidationTarget * 100, 1))
                : 0;

            return new DailyAchievementStatsDto
            {
                FromDate = fromDate,
                ToDate = toDate,
                TotalStudents = totalStudents,
                StudentsWithTargets = studentsWithTargets,
                Memorization = new AchievementCategoryDto
                {
                    Achieved = Math.Round(memorizationAchieved, 1),
                    Target = totalMemorizationTarget,
                    Percentage = memPercentage,
                    Unit = "سطر"
                },
                Revision = new AchievementCategoryDto
                {
                    Achieved = Math.Round(revisionPagesAchieved, 1),
                    Target = totalRevisionTarget,
                    Percentage = revPercentage,
                    Unit = "وجه"
                },
                Consolidation = new AchievementCategoryDto
                {
                    Achieved = Math.Round(consolidationPagesAchieved, 1),
                    Target = totalConsolidationTarget,
                    Percentage = conPercentage,
                    Unit = "وجه"
                },
                WeekSummary = new WeekSummaryDto
                {
                    Days = daySummaries,
                    DaysTargetMet = daySummaries.Count(d => d.TargetMet),
                    TotalDays = daySummaries.Count
                }
            };
        }
    }
}

