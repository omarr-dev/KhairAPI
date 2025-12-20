using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using KhairAPI.Services.Interfaces;
using KhairAPI.Core.Helpers;

namespace KhairAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "TeacherOrSupervisor")]
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

            if (_currentUserService.IsTeacher)
            {
                teacherId = await _currentUserService.GetTeacherIdAsync();
                if (!teacherId.HasValue)
                    return Unauthorized(new { message = AppConstants.ErrorMessages.CannotIdentifyTeacher });
            }

            var stats = await _statisticsService.GetDashboardStatsAsync(teacherId);
            return Ok(stats);
        }

        [HttpGet("reports")]
        public async Task<IActionResult> GetReportStats(
            [FromQuery] string dateRange = "week",
            [FromQuery] int? halaqaId = null)
        {
                int? teacherId = null;

            if (_currentUserService.IsTeacher)
            {
                teacherId = await _currentUserService.GetTeacherIdAsync();
                if (!teacherId.HasValue)
                    return Unauthorized(new { message = AppConstants.ErrorMessages.CannotIdentifyTeacher });
            }

            var stats = await _statisticsService.GetReportStatsAsync(dateRange, halaqaId, teacherId);
            return Ok(stats);
        }

        [HttpGet("system-wide-stats")]
        public async Task<IActionResult> GetSystemWideStats()
        {
            var stats = await _statisticsService.GetSystemWideStatsAsync();
                return Ok(stats);
        }

        [HttpGet("supervisor-dashboard")]
        [Authorize(Policy = "SupervisorOnly")]
        public async Task<IActionResult> GetSupervisorDashboard()
        {
            var dashboard = await _statisticsService.GetSupervisorDashboardAsync();
                return Ok(dashboard);
        }

        [HttpGet("halaqa-ranking")]
        [Authorize(Policy = "SupervisorOnly")]
        public async Task<IActionResult> GetHalaqaRanking([FromQuery] int days = 7, [FromQuery] int limit = 10)
        {
            var rankings = await _statisticsService.GetHalaqaRankingAsync(days, limit);
                return Ok(rankings);
        }

        [HttpGet("top-halaqat")]
        public async Task<IActionResult> GetTopHalaqat()
        {
            var rankings = await _statisticsService.GetTopHalaqatAsync();
                return Ok(rankings);
        }

        [HttpGet("teacher-ranking")]
        [Authorize(Policy = "SupervisorOnly")]
        public async Task<IActionResult> GetTeacherRanking([FromQuery] int days = 7, [FromQuery] int limit = 10)
        {
            var rankings = await _statisticsService.GetTeacherRankingAsync(days, limit);
                return Ok(rankings);
        }

        [HttpGet("at-risk-students")]
        [Authorize(Policy = "SupervisorOnly")]
        public async Task<IActionResult> GetAtRiskStudents([FromQuery] int limit = 20)
        {
            var atRiskStudents = await _statisticsService.GetAtRiskStudentsAsync(limit);
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
                
            var teacherId = await _currentUserService.GetTeacherIdAsync();
                if (!teacherId.HasValue)
                return Unauthorized(new { message = AppConstants.ErrorMessages.CannotIdentifyTeacher });

            var atRiskStudents = await _statisticsService.GetTeacherAtRiskStudentsAsync(teacherId.Value, limit);
                return Ok(atRiskStudents);
        }

        [HttpGet("attendance-trends")]
        [Authorize(Policy = "SupervisorOnly")]
        public async Task<IActionResult> GetAttendanceTrends([FromQuery] int days = 30)
        {
            var trends = await _statisticsService.GetAttendanceTrendsAsync(days);
                return Ok(trends);
        }

        [HttpGet("progress-trends")]
        [Authorize(Policy = "SupervisorOnly")]
        public async Task<IActionResult> GetProgressTrends([FromQuery] int days = 30)
        {
            var trends = await _statisticsService.GetProgressTrendsAsync(days);
                return Ok(trends);
        }
    }
}
