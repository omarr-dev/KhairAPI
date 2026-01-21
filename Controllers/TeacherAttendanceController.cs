using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using KhairAPI.Models.DTOs;
using KhairAPI.Services.Interfaces;
using KhairAPI.Core.Helpers;

namespace KhairAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = AppConstants.Policies.HalaqaSupervisorOrHigher)]
    public class TeacherAttendanceController : ControllerBase
    {
        private readonly ITeacherAttendanceService _teacherAttendanceService;
        private readonly ICurrentUserService _currentUserService;

        public TeacherAttendanceController(
            ITeacherAttendanceService teacherAttendanceService,
            ICurrentUserService currentUserService)
        {
            _teacherAttendanceService = teacherAttendanceService;
            _currentUserService = currentUserService;
        }

        [HttpGet("today")]
        public async Task<IActionResult> GetTodayAttendance()
        {
            // HalaqaSupervisors only see attendance for their assigned halaqas
            List<int>? halaqaFilter = null;
            if (_currentUserService.IsHalaqaSupervisor)
            {
                halaqaFilter = await _currentUserService.GetSupervisedHalaqaIdsAsync();
            }
            
            var response = await _teacherAttendanceService.GetTodayAttendanceAsync(halaqaFilter);
            return Ok(response);
        }

        [HttpPost("bulk")]
        public async Task<IActionResult> SaveBulkAttendance([FromBody] BulkTeacherAttendanceDto dto)
        {
            // HalaqaSupervisors can only save attendance for their assigned halaqas
            if (_currentUserService.IsHalaqaSupervisor)
            {
                var supervisedHalaqaIds = await _currentUserService.GetSupervisedHalaqaIdsAsync() ?? new List<int>();
                var requestedHalaqaIds = dto.Attendance.Select(a => a.HalaqaId).Distinct();
                
                if (requestedHalaqaIds.Any(id => !supervisedHalaqaIds.Contains(id)))
                    return Forbid();
            }
            
            await _teacherAttendanceService.SaveBulkAttendanceAsync(dto);
            return Ok(new { message = AppConstants.SuccessMessages.TeacherAttendanceSaved });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAttendance(int id, [FromBody] UpdateTeacherAttendanceDto dto)
        {
            // TODO: Add halaqa access check if needed (requires getting attendance record's halaqa first)
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
            var from = fromDate.HasValue ? DateTime.SpecifyKind(fromDate.Value, DateTimeKind.Utc) : (DateTime?)null;
            var to = toDate.HasValue ? DateTime.SpecifyKind(toDate.Value, DateTimeKind.Utc) : (DateTime?)null;

            // HalaqaSupervisors only see history for teachers in their assigned halaqas
            List<int>? halaqaFilter = null;
            if (_currentUserService.IsHalaqaSupervisor)
            {
                halaqaFilter = await _currentUserService.GetSupervisedHalaqaIdsAsync();
            }
            
            var records = await _teacherAttendanceService.GetTeacherAttendanceHistoryAsync(teacherId, from, to, halaqaFilter);
            return Ok(records);
        }

        [HttpGet("monthly-report")]
        public async Task<IActionResult> GetMonthlyReport([FromQuery] int year, [FromQuery] int month)
        {
            // HalaqaSupervisors only see reports for their assigned halaqas
            List<int>? halaqaFilter = null;
            if (_currentUserService.IsHalaqaSupervisor)
            {
                halaqaFilter = await _currentUserService.GetSupervisedHalaqaIdsAsync();
            }
            
            var report = await _teacherAttendanceService.GetMonthlyReportAsync(year, month, halaqaFilter);
            return Ok(report);
        }
    }
}
