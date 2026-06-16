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
        private readonly ITenantService _tenantService;

        public StatisticsService(AppDbContext context, ICacheService cache, ITenantService tenantService)
        {
            _context = context;
            _cache = cache;
            _tenantService = tenantService;
        }

        private int CurrentAssociationId => _tenantService.CurrentAssociationId ?? 0;

        private static string FilterKey(List<int>? halaqaFilter) =>
            halaqaFilter != null ? string.Join(",", halaqaFilter) : "all";

        public async Task<DashboardStatsDto> GetDashboardStatsAsync(int? teacherId = null, List<int>? halaqaFilter = null)
        {
            var cacheKey = $"dashboard_stats_{CurrentAssociationId}_{teacherId ?? 0}_{FilterKey(halaqaFilter)}";

            return await _cache.GetOrCreateAsync(
                cacheKey,
                async () => await GetDashboardStatsInternalAsync(teacherId, halaqaFilter),
                Short,
                size: 2);
        }

        private async Task<DashboardStatsDto> GetDashboardStatsInternalAsync(int? teacherId = null, List<int>? halaqaFilter = null)
        {
            var today = DateTime.UtcNow.Date;

            var studentQuery = _context.Students.AsNoTracking().AsQueryable();
            var progressQuery = _context.ProgressRecords.AsNoTracking().Where(p => p.Date == today);
            var attendanceQuery = _context.Attendances.AsNoTracking().Where(a => a.Date == today);

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

            // Distinct teachers assigned to at least one halaqa (scoped to supervised halaqat if filtered)
            var assignedTeachersQuery = _context.HalaqaTeachers.AsNoTracking().AsQueryable();
            if (halaqaFilter != null)
            {
                assignedTeachersQuery = assignedTeachersQuery.Where(ht => halaqaFilter.Contains(ht.HalaqaId));
            }
            var assignedTeachers = await assignedTeachersQuery
                .Select(ht => ht.TeacherId)
                .Distinct()
                .CountAsync();

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
                AssignedTeachers = assignedTeachers,
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

            // SQL aggregates instead of materializing every record in memory.
            // Quality/Status/Type are stored as strings, so averages are derived
            // in memory from grouped counts.
            var dailyProgressAgg = await progressQuery
                .GroupBy(p => new { p.Date, p.Type })
                .Select(g => new { g.Key.Date, g.Key.Type, Count = g.Count() })
                .ToListAsync();

            var dailyAttendanceAgg = await attendanceQuery
                .GroupBy(a => new { a.Date, a.Status })
                .Select(g => new { g.Key.Date, g.Key.Status, Count = g.Count() })
                .ToListAsync();

            var studentQualityAgg = await progressQuery
                .GroupBy(p => new { p.StudentId, p.Quality })
                .Select(g => new { g.Key.StudentId, g.Key.Quality, Count = g.Count() })
                .ToListAsync();

            var totalStudents = await studentQuery.CountAsync();

            var progressByDate = dailyProgressAgg
                .GroupBy(x => x.Date.Date)
                .ToDictionary(g => g.Key, g => g.ToList());
            var attendanceByDate = dailyAttendanceAgg
                .GroupBy(x => x.Date.Date)
                .ToDictionary(g => g.Key, g => g.ToList());

            var progressData = new List<DailyChartDataDto>();
            var attendanceData = new List<DailyChartDataDto>();

            for (var date = fromDate; date <= toDate; date = date.AddDays(1))
            {
                progressByDate.TryGetValue(date, out var dayProgress);
                attendanceByDate.TryGetValue(date, out var dayAttendance);

                progressData.Add(new DailyChartDataDto
                {
                    Date = AppConstants.ArabicDayNames.GetDayName(date.DayOfWeek),
                    Memorization = dayProgress?.Where(p => p.Type == ProgressType.Memorization).Sum(p => p.Count) ?? 0,
                    Revision = dayProgress?.Where(p => p.Type == ProgressType.Revision).Sum(p => p.Count) ?? 0,
                    Rate = 0
                });

                var totalDayAttendance = dayAttendance?.Sum(a => a.Count) ?? 0;
                var presentCount = dayAttendance?.Where(a => a.Status == AttendanceStatus.Present).Sum(a => a.Count) ?? 0;
                var rate = totalDayAttendance > 0 ? (double)presentCount / totalDayAttendance * 100 : 0;

                attendanceData.Add(new DailyChartDataDto
                {
                    Date = AppConstants.ArabicDayNames.GetDayName(date.DayOfWeek),
                    Memorization = 0,
                    Revision = 0,
                    Rate = Math.Round(rate, 1)
                });
            }

            var studentProgress = studentQualityAgg
                .GroupBy(x => x.StudentId)
                .Select(g => new
                {
                    StudentId = g.Key,
                    Count = g.Sum(x => x.Count),
                    AvgQuality = g.Sum(x => (int)x.Quality * (double)x.Count) / g.Sum(x => x.Count)
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();

            var topStudentIds = studentProgress.Select(s => s.StudentId).ToList();
            var students = await _context.Students
                .AsNoTracking()
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

            var qualityCounts = studentQualityAgg
                .GroupBy(x => x.Quality)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Count));

            var qualityDistribution = new List<QualityDistributionDto>
            {
                new() { Name = "ممتاز", Value = qualityCounts.GetValueOrDefault(QualityRating.Excellent), Color = "#10B981" },
                new() { Name = "جيد جداً", Value = qualityCounts.GetValueOrDefault(QualityRating.VeryGood), Color = "#3B82F6" },
                new() { Name = "جيد", Value = qualityCounts.GetValueOrDefault(QualityRating.Good), Color = "#F59E0B" },
                new() { Name = "مقبول", Value = qualityCounts.GetValueOrDefault(QualityRating.Acceptable), Color = "#EF4444" }
            };

            var totalAttendance = dailyAttendanceAgg.Sum(a => a.Count);
            var presentTotal = dailyAttendanceAgg.Where(a => a.Status == AttendanceStatus.Present).Sum(a => a.Count);
            var avgAttendance = totalAttendance > 0 ? (double)presentTotal / totalAttendance * 100 : 0;

            var totalProgressCount = studentQualityAgg.Sum(x => x.Count);
            var avgQuality = totalProgressCount > 0
                ? Math.Round(4 - studentQualityAgg.Sum(x => (int)x.Quality * (double)x.Count) / totalProgressCount + 1, 1)
                : 0;

            return new ReportStatsDto
            {
                TotalStudents = totalStudents,
                AverageAttendance = Math.Round(avgAttendance, 1),
                WeeklyMemorization = dailyProgressAgg.Where(p => p.Type == ProgressType.Memorization).Sum(p => p.Count),
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
                ForParams(SystemWideStats, CurrentAssociationId),
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
                .AsNoTracking()
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
            var cacheKey = ForParams(SupervisorDashboard, CurrentAssociationId, FilterKey(halaqaFilter));

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
            var todayAttendance = await _context.Attendances.AsNoTracking().Where(a => a.Date == today).ToListAsync();
            var todayProgress = await _context.ProgressRecords.AsNoTracking().Where(p => p.Date == today).ToListAsync();

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
            var cacheKey = ForParams(HalaqaRanking, CurrentAssociationId, days, limit, FilterKey(halaqaFilter));

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

            var halaqatQuery = _context.Halaqat
                .AsNoTracking()
                .Where(h => h.IsActive);

            var attendanceQuery = _context.Attendances
                .AsNoTracking()
                .Where(a => a.Date >= fromDate && a.Date <= today);

            var progressQuery = _context.ProgressRecords
                .AsNoTracking()
                .Where(p => p.Date >= fromDate && p.Date <= today);

            if (halaqaFilter != null)
            {
                halaqatQuery = halaqatQuery.Where(h => halaqaFilter.Contains(h.Id));
                attendanceQuery = attendanceQuery.Where(a => halaqaFilter.Contains(a.HalaqaId));
                progressQuery = progressQuery.Where(p => halaqaFilter.Contains(p.HalaqaId));
            }

            // Projection with SQL subquery counts instead of loading full entity graphs
            var halaqat = await halaqatQuery
                .Select(h => new
                {
                    h.Id,
                    h.Name,
                    StudentCount = h.StudentHalaqat.Count(sh => sh.IsActive),
                    TeacherCount = h.HalaqaTeachers.Count
                })
                .ToListAsync();

            // SQL aggregates grouped by halaqa instead of per-halaqa in-memory scans
            var attendanceByHalaqa = (await attendanceQuery
                .GroupBy(a => a.HalaqaId)
                .Select(g => new
                {
                    HalaqaId = g.Key,
                    Total = g.Count(),
                    Present = g.Count(a => a.Status == AttendanceStatus.Present)
                })
                .ToListAsync())
                .ToDictionary(x => x.HalaqaId);

            var progressByHalaqaType = await progressQuery
                .GroupBy(p => new { p.HalaqaId, p.Type })
                .Select(g => new
                {
                    g.Key.HalaqaId,
                    g.Key.Type,
                    Count = g.Count(),
                    Verses = g.Sum(p => p.ToVerse - p.FromVerse + 1)
                })
                .ToListAsync();

            var progressByHalaqa = progressByHalaqaType
                .GroupBy(x => x.HalaqaId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var rankings = halaqat.Select(h =>
            {
                attendanceByHalaqa.TryGetValue(h.Id, out var att);
                progressByHalaqa.TryGetValue(h.Id, out var halaqaProgress);

                var attendanceRate = att != null && att.Total > 0
                    ? (double)att.Present / att.Total * 100
                    : 0;

                var totalVerses = halaqaProgress?.Sum(p =>
                    p.Type == ProgressType.Memorization ? (double)p.Verses : p.Verses * 0.5) ?? 0;

                var progressCount = halaqaProgress?.Sum(p => p.Count) ?? 0;
                var versesPerStudent = h.StudentCount > 0 ? totalVerses / h.StudentCount : 0;
                var progressScore = Math.Min(versesPerStudent * 2, 40);
                var score = (attendanceRate * 0.6) + (progressScore * 0.4);

                return new HalaqaRankingDto
                {
                    Id = h.Id,
                    Name = h.Name,
                    StudentCount = h.StudentCount,
                    TeacherCount = h.TeacherCount,
                    AttendanceRate = Math.Round(attendanceRate, 1),
                    WeeklyProgress = progressCount,
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
            var cacheKey = ForParams(TeacherRanking, CurrentAssociationId, days, limit, FilterKey(halaqaFilter));

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

            // Active student assignments per teacher (projection, no entity graphs)
            var assignmentsQuery = _context.StudentHalaqat
                .AsNoTracking()
                .Where(sh => sh.IsActive);

            if (halaqaFilter != null)
            {
                assignmentsQuery = assignmentsQuery.Where(sh => halaqaFilter.Contains(sh.HalaqaId));
            }

            var assignments = await assignmentsQuery
                .Select(sh => new { sh.TeacherId, sh.StudentId, TeacherName = sh.Teacher!.FullName })
                .ToListAsync();

            var teacherGroups = assignments
                .GroupBy(a => a.TeacherId)
                .Select(g => new
                {
                    TeacherId = g.Key,
                    FullName = g.First().TeacherName,
                    StudentIds = g.Select(a => a.StudentId).ToHashSet()
                })
                .ToList();

            // Attendance aggregated per student in SQL, summed per teacher in memory
            var attendanceByStudent = (await _context.Attendances
                .AsNoTracking()
                .Where(a => a.Date >= fromDate && a.Date <= today)
                .GroupBy(a => a.StudentId)
                .Select(g => new
                {
                    StudentId = g.Key,
                    Total = g.Count(),
                    Present = g.Count(a => a.Status == AttendanceStatus.Present)
                })
                .ToListAsync())
                .ToDictionary(x => x.StudentId);

            // Progress aggregated per (teacher, type, quality) in SQL.
            // Quality is stored as a string, so the average is computed in memory
            // from these grouped counts.
            var progressGroups = await _context.ProgressRecords
                .AsNoTracking()
                .Where(p => p.Date >= fromDate && p.Date <= today && p.TeacherId != null)
                .GroupBy(p => new { p.TeacherId, p.Type, p.Quality })
                .Select(g => new
                {
                    g.Key.TeacherId,
                    g.Key.Type,
                    g.Key.Quality,
                    Count = g.Count(),
                    Verses = g.Sum(p => p.ToVerse - p.FromVerse + 1)
                })
                .ToListAsync();

            var progressByTeacher = progressGroups
                .GroupBy(x => x.TeacherId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            var rankings = new List<TeacherRankingDto>();

            foreach (var teacher in teacherGroups)
            {
                var studentIds = teacher.StudentIds;

                long attTotal = 0, attPresent = 0;
                foreach (var studentId in studentIds)
                {
                    if (attendanceByStudent.TryGetValue(studentId, out var att))
                    {
                        attTotal += att.Total;
                        attPresent += att.Present;
                    }
                }

                var attendanceRate = attTotal > 0
                    ? (double)attPresent / attTotal * 100
                    : 0;

                progressByTeacher.TryGetValue(teacher.TeacherId, out var teacherProgress);
                var progressCount = teacherProgress?.Sum(p => p.Count) ?? 0;

                var avgQuality = progressCount > 0
                    ? teacherProgress!.Sum(p => (4 - (int)p.Quality) * (double)p.Count) / progressCount
                    : 0;

                var totalVerses = teacherProgress?.Sum(p =>
                    p.Type == ProgressType.Memorization ? (double)p.Verses : p.Verses * 0.5) ?? 0;

                var versesPerStudent = studentIds.Count > 0 ? totalVerses / studentIds.Count : 0;
                var progressScore = Math.Min(versesPerStudent * 2, 30);
                var score = (attendanceRate * 0.5) + progressScore + (avgQuality * 5);

                rankings.Add(new TeacherRankingDto
                {
                    Id = teacher.TeacherId,
                    FullName = teacher.FullName,
                    StudentCount = studentIds.Count,
                    StudentAttendanceRate = Math.Round(attendanceRate, 1),
                    WeeklyProgress = progressCount,
                    AverageQuality = Math.Round(avgQuality, 1),
                    Score = Math.Round(score, 1)
                });
            }

            return rankings.OrderByDescending(r => r.Score).Take(limit).ToList();
        }

        public async Task<List<AtRiskStudentDto>> GetAtRiskStudentsAsync(int limit = 20, List<int>? halaqaFilter = null)
        {
            var cacheKey = ForParams(AtRiskStudents, CurrentAssociationId, limit, FilterKey(halaqaFilter));

            return await _cache.GetOrCreateAsync(
                cacheKey,
                async () => await GetAtRiskStudentsInternalAsync(limit, halaqaFilter),
                Short,
                size: 2);
        }

        private async Task<List<AtRiskStudentDto>> GetAtRiskStudentsInternalAsync(int limit = 20, List<int>? halaqaFilter = null)
        {
            IQueryable<StudentHalaqa> assignments = _context.StudentHalaqat;

            if (halaqaFilter != null)
            {
                assignments = assignments.Where(sh => halaqaFilter.Contains(sh.HalaqaId));
            }

            return await ComputeAtRiskStudentsAsync(assignments, limit);
        }

        public async Task<List<AtRiskStudentDto>> GetTeacherAtRiskStudentsAsync(int teacherId, int limit = 10)
        {
            var assignments = _context.StudentHalaqat.Where(sh => sh.TeacherId == teacherId);
            return await ComputeAtRiskStudentsAsync(assignments, limit);
        }

        private async Task<List<AtRiskStudentDto>> ComputeAtRiskStudentsAsync(IQueryable<StudentHalaqa> assignmentsQuery, int limit)
        {
            var today = DateTime.UtcNow.Date;
            var fromDate = today.AddDays(-30);

            // Single projection query: no entity graphs, just the fields needed
            var assignments = await assignmentsQuery
                .AsNoTracking()
                .Where(sh => sh.IsActive)
                .Select(sh => new
                {
                    sh.StudentId,
                    sh.Student!.FirstName,
                    sh.Student.LastName,
                    StudentCreatedAt = sh.Student.CreatedAt,
                    HalaqaName = sh.Halaqa!.Name,
                    HalaqaActiveDays = sh.Halaqa.ActiveDays,
                    TeacherName = sh.Teacher!.FullName,
                    sh.EnrollmentDate
                })
                .ToListAsync();

            // First active assignment per student (students may have multiple)
            var assignmentByStudent = assignments
                .GroupBy(a => a.StudentId)
                .ToDictionary(g => g.Key, g => g.First());

            var studentIds = assignmentByStudent.Keys.ToHashSet();
            if (studentIds.Count == 0)
                return new List<AtRiskStudentDto>();

            // Attendance for the 30-day window, keyed per student per day for O(1) checks
            var attendanceRows = await _context.Attendances
                .AsNoTracking()
                .Where(a => studentIds.Contains(a.StudentId) && a.Date >= fromDate && a.Date <= today)
                .Select(a => new { a.StudentId, a.Date, a.Status })
                .ToListAsync();

            var attendanceByStudent = attendanceRows
                .GroupBy(a => a.StudentId)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(a => a.Date.Date).ToDictionary(d => d.Key, d => d.First().Status));

            // Latest progress date per student via SQL aggregate (bounded output)
            var lastProgressByStudent = (await _context.ProgressRecords
                .AsNoTracking()
                .Where(p => studentIds.Contains(p.StudentId))
                .GroupBy(p => p.StudentId)
                .Select(g => new { StudentId = g.Key, LastDate = g.Max(p => p.Date) })
                .ToListAsync())
                .ToDictionary(x => x.StudentId, x => x.LastDate);

            var atRiskStudents = new List<AtRiskStudentDto>();

            foreach (var (studentId, assignment) in assignmentByStudent)
            {
                // For new students without progress, use their creation date instead of 999
                var daysSinceProgress = lastProgressByStudent.TryGetValue(studentId, out var lastProgressDate)
                    ? (today - lastProgressDate).Days
                    : (today - assignment.StudentCreatedAt.Date).Days;

                // Simple logic: Count consecutive absences on scheduled days only (ignore missing records)
                var activeDays = ParseActiveDays(assignment.HalaqaActiveDays);
                var enrollmentDate = assignment.EnrollmentDate.Date;
                var consecutiveAbsences = 0;
                var currentCheckDate = today;
                attendanceByStudent.TryGetValue(studentId, out var attendanceByDate);

                // Go backwards through scheduled days and count consecutive absences
                while ((today - currentCheckDate).Days < 30 && currentCheckDate >= enrollmentDate)
                {
                    if (activeDays.Contains((int)currentCheckDate.DayOfWeek))
                    {
                        // Only count explicit Absent records (ignore missing records)
                        if (attendanceByDate != null && attendanceByDate.TryGetValue(currentCheckDate, out var status))
                        {
                            if (status == AttendanceStatus.Absent)
                            {
                                consecutiveAbsences++;
                            }
                            else if (status == AttendanceStatus.Present)
                            {
                                // Stop counting when we hit a present record
                                break;
                            }
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
                        Id = studentId,
                        FullName = $"{assignment.FirstName} {assignment.LastName}",
                        HalaqaName = assignment.HalaqaName,
                        TeacherName = assignment.TeacherName,
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
                // - If today IS an active day but progress not yet recorded, check the previous active day
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
                    else
                    {
                        // Today IS an active day but progress not yet recorded.
                        // Streak is still alive if they completed the previous active day.
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

