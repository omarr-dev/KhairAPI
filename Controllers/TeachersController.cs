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
    public class TeachersController : ControllerBase
    {
        private readonly ITeacherService _teacherService;

        public TeachersController(ITeacherService teacherService)
        {
            _teacherService = teacherService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTeachers()
        {
            var teachers = await _teacherService.GetAllTeachersAsync();
            return Ok(teachers);
        }

        [HttpGet("paginated")]
        [Authorize(Policy = "SupervisorOnly")]
        public async Task<IActionResult> GetTeachersPaginated([FromQuery] TeacherFilterDto filter)
        {
            var result = await _teacherService.GetTeachersPaginatedAsync(filter);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTeacherById(int id)
        {
            var teacher = await _teacherService.GetTeacherByIdAsync(id);
            if (teacher == null)
                return NotFound(new { message = AppConstants.ErrorMessages.TeacherNotFound });

            return Ok(teacher);
        }

        [HttpGet("halaqa/{halaqaId}")]
        public async Task<IActionResult> GetTeachersByHalaqa(int halaqaId)
        {
            var teachers = await _teacherService.GetTeachersByHalaqaAsync(halaqaId);
            return Ok(teachers);
        }

        [HttpPost]
        [Authorize(Policy = "SupervisorOnly")]
        public async Task<IActionResult> CreateTeacher([FromBody] CreateTeacherDto dto)
        {
            var teacher = await _teacherService.CreateTeacherAsync(dto);
            return CreatedAtAction(nameof(GetTeacherById), new { id = teacher.Id }, teacher);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "SupervisorOnly")]
        public async Task<IActionResult> UpdateTeacher(int id, [FromBody] UpdateTeacherDto dto)
        {
            var success = await _teacherService.UpdateTeacherAsync(id, dto);
            if (!success)
                return NotFound(new { message = AppConstants.ErrorMessages.TeacherNotFound });

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "SupervisorOnly")]
        public async Task<IActionResult> DeleteTeacher(int id)
        {
            var success = await _teacherService.DeleteTeacherAsync(id);
            if (!success)
                return NotFound(new { message = AppConstants.ErrorMessages.TeacherNotFound });

            return NoContent();
        }

        [HttpPost("{teacherId}/assign-halaqa/{halaqaId}")]
        [Authorize(Policy = "SupervisorOnly")]
        public async Task<IActionResult> AssignTeacherToHalaqa(int teacherId, int halaqaId, [FromQuery] bool isPrimary = false)
        {
            await _teacherService.AssignTeacherToHalaqaAsync(teacherId, halaqaId, isPrimary);
            return Ok(new { message = AppConstants.SuccessMessages.TeacherAssigned });
        }

        [HttpGet("{teacherId}/halaqat")]
        public async Task<IActionResult> GetTeacherHalaqat(int teacherId)
        {
            var halaqat = await _teacherService.GetTeacherHalaqatAsync(teacherId);
            return Ok(halaqat);
        }

        [HttpDelete("{teacherId}/halaqat/{halaqaId}")]
        [Authorize(Policy = "SupervisorOnly")]
        public async Task<IActionResult> RemoveTeacherFromHalaqa(int teacherId, int halaqaId)
        {
            var success = await _teacherService.RemoveTeacherFromHalaqaAsync(teacherId, halaqaId);
            if (!success)
                return NotFound(new { message = AppConstants.ErrorMessages.AssignmentNotFound });

            return Ok(new { message = AppConstants.SuccessMessages.TeacherRemovedFromHalaqa });
        }
    }
}
