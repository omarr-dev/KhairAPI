using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using KhairAPI.Models.DTOs;
using KhairAPI.Services.Interfaces;
using KhairAPI.Core.Helpers;

namespace KhairAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "SupervisorOnly")]
    public class TeacherAttendanceController : ControllerBase
    {
        private readonly ITeacherAttendanceService _teacherAttendanceService;

        public TeacherAttendanceController(ITeacherAttendanceService teacherAttendanceService)
        {
            _teacherAttendanceService = teacherAttendanceService;
        }

        [HttpGet("today")]
        public async Task<IActionResult> GetTodayAttendance()
        {
            var response = await _teacherAttendanceService.GetTodayAttendanceAsync();
                return Ok(response);
        }

        [HttpPost("bulk")]
        public async Task<IActionResult> SaveBulkAttendance([FromBody] BulkTeacherAttendanceDto dto)
        {
            await _teacherAttendanceService.SaveBulkAttendanceAsync(dto);
            return Ok(new { message = AppConstants.SuccessMessages.TeacherAttendanceSaved });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAttendance(int id, [FromBody] UpdateTeacherAttendanceDto dto)
        {
            var success = await _teacherAttendanceService.UpdateAttendanceAsync(id, dto);
            if (!success)
                return NotFound(new { message = AppConstants.ErrorMessages.AttendanceNotFound });

            return Ok(new { message = AppConstants.SuccessMessages.AttendanceUpdated });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAttendance(int id)
        {
            var success = await _teacherAttendanceService.DeleteAttendanceAsync(id);
            if (!success)
                return NotFound(new { message = AppConstants.ErrorMessages.AttendanceNotFound });

                return NoContent();
        }

        [HttpGet("teacher/{teacherId}")]
        public async Task<IActionResult> GetTeacherAttendanceHistory(
            int teacherId, 
            [FromQuery] DateTime? fromDate = null, 
            [FromQuery] DateTime? toDate = null)
        {
            var records = await _teacherAttendanceService.GetTeacherAttendanceHistoryAsync(teacherId, fromDate, toDate);
                return Ok(records);
        }

        [HttpGet("monthly-report")]
        public async Task<IActionResult> GetMonthlyReport([FromQuery] int year, [FromQuery] int month)
        {
            var report = await _teacherAttendanceService.GetMonthlyReportAsync(year, month);
                return Ok(report);
        }
    }
}
