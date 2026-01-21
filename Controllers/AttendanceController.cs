using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using KhairAPI.Models.DTOs;
using KhairAPI.Models.Entities;
using KhairAPI.Services.Interfaces;
using KhairAPI.Core.Helpers;

namespace KhairAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "TeacherOrSupervisor")]
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceService _attendanceService;

        public AttendanceController(IAttendanceService attendanceService)
        {
            _attendanceService = attendanceService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAttendance([FromBody] CreateAttendanceDto dto)
        {
            var record = await _attendanceService.CreateAttendanceAsync(dto);
            return Ok(record);
        }

        [HttpPost("bulk")]
        public async Task<IActionResult> CreateBulkAttendance([FromBody] BulkAttendanceDto dto)
        {
            var result = await _attendanceService.CreateBulkAttendanceAsync(dto);
            if (result)
                return Ok(new { message = AppConstants.SuccessMessages.AttendanceSaved });

            return BadRequest(new { message = "فشل حفظ سجل الحضور" });
        }

        [HttpGet("halaqa/{halaqaId}/date/{date}")]
        public async Task<IActionResult> GetAttendanceByDate(int halaqaId, DateTime date)
        {
            date = DateTime.SpecifyKind(date, DateTimeKind.Utc);
            var summary = await _attendanceService.GetAttendanceByDateAsync(halaqaId, date);
            return Ok(summary);
        }

        [HttpGet("student/{studentId}")]
        public async Task<IActionResult> GetStudentAttendance(
            int studentId, 
            [FromQuery] DateTime? fromDate = null, 
            [FromQuery] DateTime? toDate = null)
        {
            var from = fromDate.HasValue ? DateTime.SpecifyKind(fromDate.Value, DateTimeKind.Utc) : (DateTime?)null;
            var to = toDate.HasValue ? DateTime.SpecifyKind(toDate.Value, DateTimeKind.Utc) : (DateTime?)null;
            var records = await _attendanceService.GetStudentAttendanceAsync(studentId, from, to);
            return Ok(records);
        }

        [HttpGet("student/{studentId}/summary")]
        public async Task<IActionResult> GetStudentAttendanceSummary(
            int studentId, 
            [FromQuery] DateTime fromDate, 
            [FromQuery] DateTime toDate)
        {
            fromDate = DateTime.SpecifyKind(fromDate, DateTimeKind.Utc);
            toDate = DateTime.SpecifyKind(toDate, DateTimeKind.Utc);
            var summary = await _attendanceService.GetStudentAttendanceSummaryAsync(studentId, fromDate, toDate);
            return Ok(summary);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAttendance(int id, [FromBody] UpdateAttendanceDto dto)
        {
            var result = await _attendanceService.UpdateAttendanceAsync(id, dto.Status, dto.Notes);
            if (result)
                return Ok(new { message = AppConstants.SuccessMessages.AttendanceUpdated });

            return NotFound(new { message = AppConstants.ErrorMessages.AttendanceNotFound });
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "SupervisorOnly")]
        public async Task<IActionResult> DeleteAttendance(int id)
        {
            var result = await _attendanceService.DeleteAttendanceAsync(id);
            if (result)
                return NoContent();

            return NotFound(new { message = AppConstants.ErrorMessages.AttendanceNotFound });
        }
    }

    public class UpdateAttendanceDto
    {
        public AttendanceStatus Status { get; set; }
        public string? Notes { get; set; }
    }
}
