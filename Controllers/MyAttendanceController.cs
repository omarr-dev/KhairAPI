using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using KhairAPI.Services.Interfaces;
using KhairAPI.Core.Helpers;

namespace KhairAPI.Controllers
{
    /// <summary>
    /// Teacher self-service attendance (check-in). Scoped to the logged-in teacher,
    /// so teachers can mark their own presence without supervisor access.
    /// </summary>
    [ApiController]
    [Route("api/my-attendance")]
    [Authorize(Policy = AppConstants.Policies.TeacherOrSupervisor)]
    public class MyAttendanceController : ControllerBase
    {
        private readonly ITeacherAttendanceService _teacherAttendanceService;
        private readonly ICurrentUserService _currentUserService;

        public MyAttendanceController(
            ITeacherAttendanceService teacherAttendanceService,
            ICurrentUserService currentUserService)
        {
            _teacherAttendanceService = teacherAttendanceService;
            _currentUserService = currentUserService;
        }

        /// <summary>Today's check-in status for the logged-in teacher.</summary>
        [HttpGet("today")]
        public async Task<IActionResult> GetMyStatus()
        {
            var teacherId = await _currentUserService.GetTeacherIdAsync();
            if (teacherId == null)
                return BadRequest(new { message = AppConstants.ErrorMessages.CannotIdentifyTeacher });

            var status = await _teacherAttendanceService.GetSelfAttendanceStatusAsync(teacherId.Value);
            return Ok(status);
        }

        /// <summary>Mark the logged-in teacher present in a single halaqa active today.</summary>
        [HttpPost("check-in/{halaqaId:int}")]
        public async Task<IActionResult> CheckIn(int halaqaId)
        {
            var teacherId = await _currentUserService.GetTeacherIdAsync();
            if (teacherId == null)
                return BadRequest(new { message = AppConstants.ErrorMessages.CannotIdentifyTeacher });

            var result = await _teacherAttendanceService.SelfCheckInAsync(teacherId.Value, halaqaId);
            return Ok(result);
        }

        /// <summary>Record the logged-in teacher's departure time for a single halaqa today.</summary>
        [HttpPost("check-out/{halaqaId:int}")]
        public async Task<IActionResult> CheckOut(int halaqaId)
        {
            var teacherId = await _currentUserService.GetTeacherIdAsync();
            if (teacherId == null)
                return BadRequest(new { message = AppConstants.ErrorMessages.CannotIdentifyTeacher });

            var result = await _teacherAttendanceService.SelfCheckOutAsync(teacherId.Value, halaqaId);
            return Ok(result);
        }
    }
}
