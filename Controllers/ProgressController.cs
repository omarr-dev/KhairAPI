using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using KhairAPI.Models.DTOs;
using KhairAPI.Services.Interfaces;
using KhairAPI.Core.Helpers;

namespace KhairAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "TeacherOrSupervisor")]
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

            var record = await _progressService.CreateProgressRecordAsync(dto);
            return CreatedAtAction(nameof(GetDailyProgress), new { date = record.Date }, record);
        }

        [HttpGet("daily/{date}")]
        public async Task<IActionResult> GetDailyProgress(DateTime date)
        {
            int? teacherId = null;

            if (_currentUserService.IsTeacher)
            {
                teacherId = await _currentUserService.GetTeacherIdAsync();
                if (!teacherId.HasValue)
                    return Unauthorized(new { message = AppConstants.ErrorMessages.CannotIdentifyTeacher });
            }

            var summary = await _progressService.GetDailyProgressSummaryAsync(date, teacherId);
            return Ok(summary);
        }

        [HttpGet("student/{studentId}")]
        public async Task<IActionResult> GetStudentProgress(int studentId, [FromQuery] DateTime? fromDate = null)
        {
            var progress = await _progressService.GetProgressByStudentAsync(studentId, fromDate);
            return Ok(progress);
        }

        [HttpGet("student/{studentId}/summary")]
        public async Task<IActionResult> GetStudentProgressSummary(int studentId)
        {
            var summary = await _progressService.GetStudentProgressSummaryAsync(studentId);
            return Ok(summary);
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
            return await GetDailyProgress(DateTime.Today);
        }
    }
}
