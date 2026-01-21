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
            [FromQuery] int? halaqaId = null)
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

            var stats = await _statisticsService.GetReportStatsAsync(dateRange, halaqaId, teacherId, supervisedHalaqaIds);
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
