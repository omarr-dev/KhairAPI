using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using KhairAPI.Models.DTOs;
using KhairAPI.Services.Interfaces;
using KhairAPI.Core.Helpers;

namespace KhairAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = AppConstants.Policies.AnyRole)]
    public class ProgressController : ControllerBase
    {
        private readonly IProgressService _progressService;
        private readonly ICurrentUserService _currentUserService;

        public ProgressController(IProgressService progressService, ICurrentUserService currentUserService)
        {
            _progressService = progressService;
            _currentUserService = currentUserService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateProgressRecord([FromBody] CreateProgressRecordDto dto)
        {
            if (_currentUserService.IsTeacher)
            {
                var teacherId = await _currentUserService.GetTeacherIdAsync();
                if (!teacherId.HasValue || teacherId.Value != dto.TeacherId)
                    return Forbid();
            }
            
            // HalaqaSupervisors can only create progress in their assigned halaqas
            if (_currentUserService.IsHalaqaSupervisor)
            {
                var canAccess = await _currentUserService.CanAccessHalaqaAsync(dto.HalaqaId);
                if (!canAccess)
                    return Forbid();
            }

            var record = await _progressService.CreateProgressRecordAsync(dto);
            return CreatedAtAction(nameof(GetDailyProgress), new { date = record.Date }, record);
        }

        [HttpGet("daily/{date}")]
        public async Task<IActionResult> GetDailyProgress(DateTime date)
        {
            date = DateTime.SpecifyKind(date, DateTimeKind.Utc);
            int? teacherId = null;
            List<int>? halaqaFilter = null;

            if (_currentUserService.IsTeacher)
            {
                teacherId = await _currentUserService.GetTeacherIdAsync();
                if (!teacherId.HasValue)
                    return Unauthorized(new { message = AppConstants.ErrorMessages.CannotIdentifyTeacher });
            }
            else if (_currentUserService.IsHalaqaSupervisor)
            {
                halaqaFilter = await _currentUserService.GetSupervisedHalaqaIdsAsync();
            }

            var summary = await _progressService.GetDailyProgressSummaryAsync(date, teacherId, halaqaFilter);
            return Ok(summary);
        }

        [HttpGet("student/{studentId}")]
        public async Task<IActionResult> GetStudentProgress(int studentId, [FromQuery] DateTime? fromDate = null)
        {
            var from = fromDate.HasValue ? DateTime.SpecifyKind(fromDate.Value, DateTimeKind.Utc) : (DateTime?)null;
            var progress = await _progressService.GetProgressByStudentAsync(studentId, from);
            return Ok(progress);
        }

        [HttpGet("student/{studentId}/summary")]
        public async Task<IActionResult> GetStudentProgressSummary(int studentId)
        {
            var summary = await _progressService.GetStudentProgressSummaryAsync(studentId);
            return Ok(summary);
        }

        [HttpGet("student/{studentId}/last")]
        public async Task<IActionResult> GetLastProgressByType(int studentId, [FromQuery] int type)
        {
            // Validate input parameters
            if (studentId <= 0)
                return BadRequest(new { message = "معرف الطالب غير صحيح" });

            if (type < 0 || type > 2)
                return BadRequest(new { message = "نوع التسميع غير صحيح. القيم المسموحة: 0 (حفظ)، 1 (مراجعة)، 2 (تثبيت)" });

            // Authorization: If teacher, verify they have access to this student
            if (_currentUserService.IsTeacher)
            {
                var teacherId = await _currentUserService.GetTeacherIdAsync();
                if (!teacherId.HasValue)
                    return Unauthorized(new { message = AppConstants.ErrorMessages.CannotIdentifyTeacher });

                var hasAccess = await _progressService.TeacherHasAccessToStudentAsync(teacherId.Value, studentId);
                if (!hasAccess)
                    return Forbid();
            }

            var lastProgress = await _progressService.GetLastProgressByTypeAsync(studentId, type);
            return Ok(lastProgress);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProgressRecord(int id)
        {
            if (!_currentUserService.UserId.HasValue)
                return Unauthorized();

            var result = await _progressService.DeleteProgressRecordAsync(id, _currentUserService.UserId.Value);
            if (!result)
                return NotFound(new { message = AppConstants.ErrorMessages.ProgressNotFound });

            return NoContent();
        }

        [HttpGet("today")]
        public async Task<IActionResult> GetTodayProgress()
        {
            return await GetDailyProgress(DateTime.UtcNow.Date);
        }
    }
}
