using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using KhairAPI.Models.DTOs;
using KhairAPI.Services.Interfaces;
using KhairAPI.Core.Helpers;

namespace KhairAPI.Controllers
{
    /// <summary>
    /// Read-only self-service endpoints for the student portal. Every action resolves the
    /// student id from the JWT ("StudentId" claim) via <see cref="ICurrentUserService"/> and
    /// NEVER from the request, so a student can only ever read their own data.
    /// The <c>StudentOnly</c> policy keeps teachers/supervisors out (and students out of every
    /// other controller, which all require Teacher/Supervisor roles).
    /// </summary>
    [ApiController]
    [Route("api/students/me")]
    [Authorize(Policy = AppConstants.Policies.StudentOnly)]
    public class StudentPortalController : ControllerBase
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IStudentService _studentService;
        private readonly IProgressService _progressService;
        private readonly IAttendanceService _attendanceService;
        private readonly IStudentTargetService _targetService;
        private readonly IStatisticsService _statisticsService;

        public StudentPortalController(
            ICurrentUserService currentUserService,
            IStudentService studentService,
            IProgressService progressService,
            IAttendanceService attendanceService,
            IStudentTargetService targetService,
            IStatisticsService statisticsService)
        {
            _currentUserService = currentUserService;
            _studentService = studentService;
            _progressService = progressService;
            _attendanceService = attendanceService;
            _targetService = targetService;
            _statisticsService = statisticsService;
        }

        /// <summary>
        /// The current student's id from the token, or null if the token is not a student token.
        /// </summary>
        private int? MyStudentId => _currentUserService.StudentId;

        /// <summary>
        /// Full profile: basic info, halaqa/teacher, memorization position, stats,
        /// recent progress/attendance and the daily target. Powers الرئيسية and ملفي.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMyProfile()
        {
            if (!MyStudentId.HasValue)
                return Unauthorized();

            var details = await _studentService.GetStudentDetailsAsync(MyStudentId.Value);
            if (details == null)
                return NotFound(new { message = AppConstants.ErrorMessages.StudentNotFound });

            return Ok(details);
        }

        /// <summary>Memorization/revision/consolidation records (حفظي).</summary>
        [HttpGet("progress")]
        public async Task<IActionResult> GetMyProgress([FromQuery] DateTime? fromDate = null)
        {
            if (!MyStudentId.HasValue)
                return Unauthorized();

            var from = fromDate.HasValue ? DateTime.SpecifyKind(fromDate.Value, DateTimeKind.Utc) : (DateTime?)null;
            var progress = await _progressService.GetProgressByStudentAsync(MyStudentId.Value, from);
            return Ok(progress);
        }

        /// <summary>Aggregate progress totals for the student.</summary>
        [HttpGet("progress/summary")]
        public async Task<IActionResult> GetMyProgressSummary()
        {
            if (!MyStudentId.HasValue)
                return Unauthorized();

            var summary = await _progressService.GetStudentProgressSummaryAsync(MyStudentId.Value);
            return Ok(summary);
        }

        /// <summary>Attendance records for a date range (حضوري). Defaults to the last 3 months.</summary>
        [HttpGet("attendance")]
        public async Task<IActionResult> GetMyAttendance(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            if (!MyStudentId.HasValue)
                return Unauthorized();

            var from = fromDate.HasValue ? DateTime.SpecifyKind(fromDate.Value, DateTimeKind.Utc) : (DateTime?)null;
            var to = toDate.HasValue ? DateTime.SpecifyKind(toDate.Value, DateTimeKind.Utc) : (DateTime?)null;
            var records = await _attendanceService.GetStudentAttendanceAsync(MyStudentId.Value, from, to);
            return Ok(records);
        }

        /// <summary>Attendance summary (rate, present/absent/late) for a date range.</summary>
        [HttpGet("attendance/summary")]
        public async Task<IActionResult> GetMyAttendanceSummary(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            if (!MyStudentId.HasValue)
                return Unauthorized();

            var to = toDate.HasValue ? DateTime.SpecifyKind(toDate.Value, DateTimeKind.Utc) : DateTime.UtcNow.Date;
            var from = fromDate.HasValue ? DateTime.SpecifyKind(fromDate.Value, DateTimeKind.Utc) : to.AddMonths(-3);
            var summary = await _attendanceService.GetStudentAttendanceSummaryAsync(MyStudentId.Value, from, to);
            return Ok(summary);
        }

        /// <summary>The student's current daily target.</summary>
        [HttpGet("target")]
        public async Task<IActionResult> GetMyTarget()
        {
            if (!MyStudentId.HasValue)
                return Unauthorized();

            var target = await _targetService.GetTargetAsync(MyStudentId.Value);
            return Ok(target ?? new StudentTargetDto { StudentId = MyStudentId.Value });
        }

        /// <summary>Daily achievement history + streak info for a date range (calendar heatmap).</summary>
        [HttpGet("achievement-history")]
        public async Task<IActionResult> GetMyAchievementHistory(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            if (!MyStudentId.HasValue)
                return Unauthorized();

            startDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
            endDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);

            try
            {
                var history = await _targetService.GetAchievementHistoryAsync(MyStudentId.Value, startDate, endDate);
                return Ok(history);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// The student's rank within their own halaqa's streak leaderboard (إنجازاتي).
        /// Returns only the student's own entry — peers' data is never exposed.
        /// </summary>
        [HttpGet("rank")]
        public async Task<IActionResult> GetMyRank()
        {
            if (!MyStudentId.HasValue)
                return Unauthorized();

            var details = await _studentService.GetStudentDetailsAsync(MyStudentId.Value);
            if (details == null)
                return NotFound(new { message = AppConstants.ErrorMessages.StudentNotFound });

            var filter = new StreakLeaderboardFilterDto
            {
                SelectedHalaqaId = details.HalaqaId, // scope to my halaqa; null => association-wide
                Limit = int.MaxValue // include everyone so my own entry is present
            };

            var leaderboard = await _statisticsService.GetStreakLeaderboardAsync(filter);
            var mine = leaderboard.Students.FirstOrDefault(s => s.StudentId == MyStudentId.Value);

            return Ok(new MyRankDto
            {
                Rank = mine?.Rank,
                TotalInScope = leaderboard.TotalStudentsInScope,
                StudentsWithActiveStreaks = leaderboard.StudentsWithActiveStreaks,
                CurrentStreak = mine?.CurrentStreak ?? 0,
                LongestStreak = mine?.LongestStreak ?? 0,
                IsStreakActive = mine?.IsStreakActive ?? false,
                HalaqaName = details.CurrentHalaqa
            });
        }
    }

    /// <summary>The current student's standing in their halaqa leaderboard (own data only).</summary>
    public class MyRankDto
    {
        public int? Rank { get; set; }
        public int TotalInScope { get; set; }
        public int StudentsWithActiveStreaks { get; set; }
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        public bool IsStreakActive { get; set; }
        public string? HalaqaName { get; set; }
    }
}
