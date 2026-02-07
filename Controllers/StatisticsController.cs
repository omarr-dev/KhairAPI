using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using KhairAPI.Services.Interfaces;
using KhairAPI.Models.DTOs;
using KhairAPI.Core.Helpers;

namespace KhairAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = AppConstants.Policies.AnyRole)]
    public class StatisticsController : ControllerBase
    {
        private readonly IStatisticsService _statisticsService;
        private readonly ICurrentUserService _currentUserService;

        public StatisticsController(IStatisticsService statisticsService, ICurrentUserService currentUserService)
        {
            _statisticsService = statisticsService;
            _currentUserService = currentUserService;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardStats()
        {
            int? teacherId = null;
            List<int>? supervisedHalaqaIds = null;

            if (_currentUserService.IsTeacher)
            {
                teacherId = await _currentUserService.GetTeacherIdAsync();
                if (!teacherId.HasValue)
                    return Unauthorized(new { message = AppConstants.ErrorMessages.CannotIdentifyTeacher });
            }
            else if (_currentUserService.IsHalaqaSupervisor)
            {
                supervisedHalaqaIds = await _currentUserService.GetSupervisedHalaqaIdsAsync();
            }
            // Full Supervisors see all (no filtering)

            var stats = await _statisticsService.GetDashboardStatsAsync(teacherId, supervisedHalaqaIds);
            return Ok(stats);
        }

        [HttpGet("reports")]
        public async Task<IActionResult> GetReportStats(
            [FromQuery] string dateRange = "week",
            [FromQuery] int? halaqaId = null,
            [FromQuery] int? teacherId = null,
            [FromQuery] string? fromDate = null,
            [FromQuery] string? toDate = null)
        {
            int? effectiveTeacherId = teacherId;
            List<int>? supervisedHalaqaIds = null;

            if (_currentUserService.IsTeacher)
            {
                // Teachers can only see their own data
                effectiveTeacherId = await _currentUserService.GetTeacherIdAsync();
                if (!effectiveTeacherId.HasValue)
                    return Unauthorized(new { message = AppConstants.ErrorMessages.CannotIdentifyTeacher });
            }
            else if (_currentUserService.IsHalaqaSupervisor)
            {
                supervisedHalaqaIds = await _currentUserService.GetSupervisedHalaqaIdsAsync();
                // If specific halaqa requested, verify access
                if (halaqaId.HasValue && supervisedHalaqaIds != null && !supervisedHalaqaIds.Contains(halaqaId.Value))
                {
                    return Forbid();
                }
                // Supervisors can use the teacherId filter passed from frontend
            }

            // Validate custom date range if provided
            DateTime? parsedFromDate = null;
            DateTime? parsedToDate = null;

            if (dateRange == "custom")
            {
                if (string.IsNullOrEmpty(fromDate) || string.IsNullOrEmpty(toDate))
                {
                    return BadRequest(new { message = "يجب تحديد تاريخ البداية والنهاية للفترة المحددة" });
                }

                // Use exact format parsing with invariant culture for consistent behavior
                if (!DateTime.TryParseExact(fromDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, 
                        System.Globalization.DateTimeStyles.None, out var from) || 
                    !DateTime.TryParseExact(toDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, 
                        System.Globalization.DateTimeStyles.None, out var to))
                {
                    return BadRequest(new { message = "صيغة التاريخ غير صحيحة. استخدم الصيغة: YYYY-MM-DD" });
                }

                // Security: Validate date range
                var minAllowedDate = new DateTime(2020, 1, 1);
                var maxAllowedDate = DateTime.UtcNow.Date;

                if (from < minAllowedDate || to < minAllowedDate)
                {
                    return BadRequest(new { message = "التاريخ لا يمكن أن يكون قبل 2020-01-01" });
                }

                if (from > maxAllowedDate || to > maxAllowedDate)
                {
                    return BadRequest(new { message = "التاريخ لا يمكن أن يكون في المستقبل" });
                }

                if (from > to)
                {
                    return BadRequest(new { message = "تاريخ البداية يجب أن يكون قبل تاريخ النهاية" });
                }

                // Security: Limit date range to prevent performance issues
                var maxRangeDays = 365;
                if ((to - from).Days > maxRangeDays)
                {
                    return BadRequest(new { message = $"الفترة الزمنية لا يمكن أن تتجاوز {maxRangeDays} يوم" });
                }

                parsedFromDate = from;
                parsedToDate = to;
            }

            var stats = await _statisticsService.GetReportStatsAsync(
                dateRange,
                halaqaId,
                effectiveTeacherId,
                supervisedHalaqaIds,
                parsedFromDate,
                parsedToDate);
            return Ok(stats);
        }

        [HttpGet("system-wide-stats")]
        public async Task<IActionResult> GetSystemWideStats()
        {
            var stats = await _statisticsService.GetSystemWideStatsAsync();
            return Ok(stats);
        }

        [HttpGet("supervisor-dashboard")]
        [Authorize(Policy = AppConstants.Policies.HalaqaSupervisorOrHigher)]
        public async Task<IActionResult> GetSupervisorDashboard()
        {
            List<int>? supervisedHalaqaIds = null;
            if (_currentUserService.IsHalaqaSupervisor)
            {
                supervisedHalaqaIds = await _currentUserService.GetSupervisedHalaqaIdsAsync();
            }

            var dashboard = await _statisticsService.GetSupervisorDashboardAsync(supervisedHalaqaIds);
            return Ok(dashboard);
        }

        [HttpGet("halaqa-ranking")]
        [Authorize(Policy = AppConstants.Policies.HalaqaSupervisorOrHigher)]
        public async Task<IActionResult> GetHalaqaRanking([FromQuery] int days = 7, [FromQuery] int limit = 10)
        {
            List<int>? supervisedHalaqaIds = null;
            if (_currentUserService.IsHalaqaSupervisor)
            {
                supervisedHalaqaIds = await _currentUserService.GetSupervisedHalaqaIdsAsync();
            }

            var rankings = await _statisticsService.GetHalaqaRankingAsync(days, limit, supervisedHalaqaIds);
            return Ok(rankings);
        }

        [HttpGet("top-halaqat")]
        public async Task<IActionResult> GetTopHalaqat()
        {
            var rankings = await _statisticsService.GetTopHalaqatAsync();
            return Ok(rankings);
        }

        [HttpGet("teacher-ranking")]
        [Authorize(Policy = AppConstants.Policies.HalaqaSupervisorOrHigher)]
        public async Task<IActionResult> GetTeacherRanking([FromQuery] int days = 7, [FromQuery] int limit = 10)
        {
            List<int>? supervisedHalaqaIds = null;
            if (_currentUserService.IsHalaqaSupervisor)
            {
                supervisedHalaqaIds = await _currentUserService.GetSupervisedHalaqaIdsAsync();
            }

            var rankings = await _statisticsService.GetTeacherRankingAsync(days, limit, supervisedHalaqaIds);
            return Ok(rankings);
        }

        [HttpGet("at-risk-students")]
        [Authorize(Policy = AppConstants.Policies.HalaqaSupervisorOrHigher)]
        public async Task<IActionResult> GetAtRiskStudents([FromQuery] int limit = 20)
        {
            List<int>? supervisedHalaqaIds = null;
            if (_currentUserService.IsHalaqaSupervisor)
            {
                supervisedHalaqaIds = await _currentUserService.GetSupervisedHalaqaIdsAsync();
            }

            var atRiskStudents = await _statisticsService.GetAtRiskStudentsAsync(limit, supervisedHalaqaIds);
            return Ok(atRiskStudents);
        }

        [HttpGet("my-at-risk-students")]
        public async Task<IActionResult> GetMyAtRiskStudents([FromQuery] int limit = 10)
        {
            if (_currentUserService.IsSupervisor)
            {
                var allAtRiskStudents = await _statisticsService.GetAtRiskStudentsAsync(limit);
                return Ok(allAtRiskStudents);
            }
            
            if (_currentUserService.IsHalaqaSupervisor)
            {
                var supervisedHalaqaIds = await _currentUserService.GetSupervisedHalaqaIdsAsync();
                var atRiskStudents = await _statisticsService.GetAtRiskStudentsAsync(limit, supervisedHalaqaIds);
                return Ok(atRiskStudents);
            }
                
            var teacherId = await _currentUserService.GetTeacherIdAsync();
            if (!teacherId.HasValue)
                return Unauthorized(new { message = AppConstants.ErrorMessages.CannotIdentifyTeacher });

            var teacherAtRiskStudents = await _statisticsService.GetTeacherAtRiskStudentsAsync(teacherId.Value, limit);
            return Ok(teacherAtRiskStudents);
        }

        /// <summary>
        /// Gets target adoption overview statistics.
        /// تغطية نظام الأهداف
        ///
        /// - Teachers: See target coverage for their students only
        /// - HalaqaSupervisors: See target coverage for their assigned halaqat
        /// - Supervisors: See target coverage for all students
        ///
        /// Optional: Filter by specific halaqa (validated for access)
        /// Optional: Filter by specific teacher (for supervisors)
        /// Optional: Include per-halaqa breakdown
        /// </summary>
        /// <param name="halaqaId">Optional: Filter to a specific halaqa</param>
        /// <param name="teacherId">Optional: Filter to a specific teacher (for supervisors)</param>
        /// <param name="includeBreakdown">Include per-halaqa breakdown (default: false)</param>
        [HttpGet("target-adoption-overview")]
        public async Task<IActionResult> GetTargetAdoptionOverview(
            [FromQuery] int? halaqaId = null,
            [FromQuery] int? teacherId = null,
            [FromQuery] bool includeBreakdown = false)
        {
            var filter = new TargetAdoptionFilterDto
            {
                SelectedHalaqaId = halaqaId,
                SelectedTeacherId = teacherId,
                IncludeHalaqaBreakdown = includeBreakdown
            };

            if (_currentUserService.IsTeacher)
            {
                // Teachers can only see their own students
                filter.TeacherId = await _currentUserService.GetTeacherIdAsync();
                if (!filter.TeacherId.HasValue)
                    return Unauthorized(new { message = AppConstants.ErrorMessages.CannotIdentifyTeacher });

                // Teachers cannot filter by halaqa - they see all their students
                if (halaqaId.HasValue)
                    return BadRequest(new { message = "المعلم لا يمكنه تحديد حلقة معينة، سيتم عرض جميع طلابه" });
            }
            else if (_currentUserService.IsHalaqaSupervisor)
            {
                // HalaqaSupervisors see only their assigned halaqat
                filter.SupervisedHalaqaIds = await _currentUserService.GetSupervisedHalaqaIdsAsync();

                // Validate access to specific halaqa if requested
                if (halaqaId.HasValue)
                {
                    if (filter.SupervisedHalaqaIds == null || !filter.SupervisedHalaqaIds.Contains(halaqaId.Value))
                    {
                        return Forbid();
                    }
                }
            }
            // Full Supervisors have no restrictions (teacherId and supervisedHalaqaIds remain null)

            var result = await _statisticsService.GetTargetAdoptionOverviewAsync(filter);

            return Ok(result);
        }

        /// <summary>
        /// Gets daily achievement statistics showing aggregated progress vs targets.
        /// إنجاز اليوم - إحصائيات الإنجاز اليومي المجمّعة
        ///
        /// - Teachers: See aggregated achievements for their students only
        /// - HalaqaSupervisors: See aggregated achievements for their assigned halaqat
        /// - Supervisors: See aggregated achievements for all students or filter by halaqa/teacher
        ///
        /// Default date range: today + last 7 days
        /// </summary>
        /// <param name="halaqaId">Optional: Filter to a specific halaqa</param>
        /// <param name="teacherId">Optional: Filter to a specific teacher (for supervisors)</param>
        /// <param name="fromDate">Optional: Start date (default: 7 days ago)</param>
        /// <param name="toDate">Optional: End date (default: today)</param>
        [HttpGet("daily-achievement")]
        public async Task<IActionResult> GetDailyAchievementStats(
            [FromQuery] int? halaqaId = null,
            [FromQuery] int? teacherId = null,
            [FromQuery] string? fromDate = null,
            [FromQuery] string? toDate = null)
        {
            // Parse and validate dates
            var today = DateTime.UtcNow.Date;
            DateTime parsedFromDate = today.AddDays(-6); // Default: last 7 days including today
            DateTime parsedToDate = today;

            if (!string.IsNullOrEmpty(fromDate))
            {
                if (!DateTime.TryParseExact(fromDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out parsedFromDate))
                {
                    return BadRequest(new { message = "صيغة تاريخ البداية غير صحيحة. استخدم الصيغة: YYYY-MM-DD" });
                }
            }

            if (!string.IsNullOrEmpty(toDate))
            {
                if (!DateTime.TryParseExact(toDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out parsedToDate))
                {
                    return BadRequest(new { message = "صيغة تاريخ النهاية غير صحيحة. استخدم الصيغة: YYYY-MM-DD" });
                }
            }

            // Security: Validate date range
            var minAllowedDate = new DateTime(2020, 1, 1);
            var maxAllowedDate = today;

            if (parsedFromDate < minAllowedDate || parsedToDate < minAllowedDate)
            {
                return BadRequest(new { message = "التاريخ لا يمكن أن يكون قبل 2020-01-01" });
            }

            if (parsedFromDate > maxAllowedDate.AddDays(1) || parsedToDate > maxAllowedDate.AddDays(1))
            {
                return BadRequest(new { message = "التاريخ لا يمكن أن يكون في المستقبل" });
            }

            if (parsedFromDate > parsedToDate)
            {
                return BadRequest(new { message = "تاريخ البداية يجب أن يكون قبل تاريخ النهاية" });
            }

            // Security: Limit date range to prevent performance issues
            const int maxRangeDays = 90;
            if ((parsedToDate - parsedFromDate).Days > maxRangeDays)
            {
                return BadRequest(new { message = $"الفترة الزمنية لا يمكن أن تتجاوز {maxRangeDays} يوم" });
            }

            var filter = new DailyAchievementFilterDto
            {
                FromDate = parsedFromDate,
                ToDate = parsedToDate,
                SelectedHalaqaId = halaqaId,
                SelectedTeacherId = teacherId
            };

            if (_currentUserService.IsTeacher)
            {
                // Teachers can only see their own students
                filter.TeacherId = await _currentUserService.GetTeacherIdAsync();
                if (!filter.TeacherId.HasValue)
                    return Unauthorized(new { message = AppConstants.ErrorMessages.CannotIdentifyTeacher });

                // Teachers cannot filter by halaqa - they see all their students
                if (halaqaId.HasValue)
                    return BadRequest(new { message = "المعلم لا يمكنه تحديد حلقة معينة" });
            }
            else if (_currentUserService.IsHalaqaSupervisor)
            {
                // HalaqaSupervisors see only their assigned halaqat
                filter.SupervisedHalaqaIds = await _currentUserService.GetSupervisedHalaqaIdsAsync();

                // Validate access to specific halaqa if requested
                if (halaqaId.HasValue)
                {
                    if (filter.SupervisedHalaqaIds == null || !filter.SupervisedHalaqaIds.Contains(halaqaId.Value))
                    {
                        return Forbid();
                    }
                }
            }
            // Full Supervisors have no restrictions (teacherId and supervisedHalaqaIds remain null)

            var result = await _statisticsService.GetDailyAchievementStatsAsync(filter);

            return Ok(result);
        }

        /// <summary>
        /// Gets streak leaderboard - students with longest consecutive progress days.
        /// أطول سلاسل الإنجاز
        ///
        /// - Teachers: See streaks for their students only
        /// - HalaqaSupervisors: See streaks for students in their assigned halaqat
        /// - Supervisors: See all students, can filter by halaqa/teacher
        ///
        /// A streak counts consecutive active halaqa days where the student had at least one progress record.
        /// </summary>
        /// <param name="halaqaId">Optional: Filter to a specific halaqa</param>
        /// <param name="teacherId">Optional: Filter to a specific teacher (for supervisors)</param>
        /// <param name="limit">Number of top students to return (default: 10, max: 100)</param>
        [HttpGet("streak-leaderboard")]
        public async Task<IActionResult> GetStreakLeaderboard(
            [FromQuery] int? halaqaId = null,
            [FromQuery] int? teacherId = null,
            [FromQuery] int limit = 10)
        {
            // Security: Validate and clamp limit
            if (limit <= 0) limit = 10;
            if (limit > 100) limit = 100;

            var filter = new StreakLeaderboardFilterDto
            {
                SelectedHalaqaId = halaqaId,
                SelectedTeacherId = teacherId,
                Limit = limit
            };

            if (_currentUserService.IsTeacher)
            {
                // Teachers see only their students
                filter.TeacherId = await _currentUserService.GetTeacherIdAsync();
                if (!filter.TeacherId.HasValue)
                    return Unauthorized(new { message = AppConstants.ErrorMessages.CannotIdentifyTeacher });

                // Teachers cannot filter by halaqa - they see all their students
                if (halaqaId.HasValue)
                    return BadRequest(new { message = "المعلم لا يمكنه تحديد حلقة معينة" });
            }
            else if (_currentUserService.IsHalaqaSupervisor)
            {
                // HalaqaSupervisors see only their assigned halaqat
                filter.SupervisedHalaqaIds = await _currentUserService.GetSupervisedHalaqaIdsAsync();

                // Validate access to specific halaqa if requested
                if (halaqaId.HasValue)
                {
                    if (filter.SupervisedHalaqaIds == null || !filter.SupervisedHalaqaIds.Contains(halaqaId.Value))
                    {
                        return Forbid();
                    }
                }
            }
            // Full Supervisors have no restrictions (teacherId and supervisedHalaqaIds remain null)

            var result = await _statisticsService.GetStreakLeaderboardAsync(filter);

            return Ok(result);
        }
    }
}
