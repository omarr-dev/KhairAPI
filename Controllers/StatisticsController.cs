using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using KhairAPI.Services.Interfaces;
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
            [FromQuery] string? fromDate = null,
            [FromQuery] string? toDate = null)
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
                // If specific halaqa requested, verify access
                if (halaqaId.HasValue && supervisedHalaqaIds != null && !supervisedHalaqaIds.Contains(halaqaId.Value))
                {
                    return Forbid();
                }
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
                teacherId, 
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
    }
}
