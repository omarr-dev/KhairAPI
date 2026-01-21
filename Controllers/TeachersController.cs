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
    public class TeachersController : ControllerBase
    {
        private readonly ITeacherService _teacherService;
        private readonly ICurrentUserService _currentUserService;

        public TeachersController(ITeacherService teacherService, ICurrentUserService currentUserService)
        {
            _teacherService = teacherService;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTeachers()
        {
            // HalaqaSupervisors only see teachers in their assigned halaqas
            if (_currentUserService.IsHalaqaSupervisor)
            {
                var supervisedHalaqaIds = await _currentUserService.GetSupervisedHalaqaIdsAsync();
                var teachers = await _teacherService.GetTeachersByHalaqasAsync(supervisedHalaqaIds ?? new List<int>());
                return Ok(teachers);
            }
            
            var allTeachers = await _teacherService.GetAllTeachersAsync();
            return Ok(allTeachers);
        }

        [HttpGet("paginated")]
        [Authorize(Policy = AppConstants.Policies.HalaqaSupervisorOrHigher)]
        public async Task<IActionResult> GetTeachersPaginated([FromQuery] TeacherFilterDto filter)
        {
            // HalaqaSupervisors can only filter by their assigned halaqas
            if (_currentUserService.IsHalaqaSupervisor)
            {
                var supervisedHalaqaIds = await _currentUserService.GetSupervisedHalaqaIdsAsync();
                filter.HalaqaIds = supervisedHalaqaIds;
            }
            
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
            // HalaqaSupervisors can only view teachers in their assigned halaqas
            if (_currentUserService.IsHalaqaSupervisor)
            {
                var canAccess = await _currentUserService.CanAccessHalaqaAsync(halaqaId);
                if (!canAccess)
                    return Forbid();
            }
            
            var teachers = await _teacherService.GetTeachersByHalaqaAsync(halaqaId);
            return Ok(teachers);
        }

        [HttpPost]
        [Authorize(Policy = AppConstants.Policies.SupervisorOnly)]
        public async Task<IActionResult> CreateTeacher([FromBody] CreateTeacherDto dto)
        {
            var teacher = await _teacherService.CreateTeacherAsync(dto);
            return CreatedAtAction(nameof(GetTeacherById), new { id = teacher.Id }, teacher);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = AppConstants.Policies.SupervisorOnly)]
        public async Task<IActionResult> UpdateTeacher(int id, [FromBody] UpdateTeacherDto dto)
        {
            var success = await _teacherService.UpdateTeacherAsync(id, dto);
            if (!success)
                return NotFound(new { message = AppConstants.ErrorMessages.TeacherNotFound });

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = AppConstants.Policies.SupervisorOnly)]
        public async Task<IActionResult> DeleteTeacher(int id)
        {
            var success = await _teacherService.DeleteTeacherAsync(id);
            if (!success)
                return NotFound(new { message = AppConstants.ErrorMessages.TeacherNotFound });

            return NoContent();
        }

        [HttpPost("{teacherId}/assign-halaqa/{halaqaId}")]
        [Authorize(Policy = AppConstants.Policies.HalaqaSupervisorOrHigher)]
        public async Task<IActionResult> AssignTeacherToHalaqa(int teacherId, int halaqaId, [FromQuery] bool isPrimary = false)
        {
            // HalaqaSupervisors can only assign to their assigned halaqas
            if (_currentUserService.IsHalaqaSupervisor)
            {
                var canAccess = await _currentUserService.CanAccessHalaqaAsync(halaqaId);
                if (!canAccess)
                    return Forbid();
            }
            
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
        [Authorize(Policy = AppConstants.Policies.HalaqaSupervisorOrHigher)]
        public async Task<IActionResult> RemoveTeacherFromHalaqa(int teacherId, int halaqaId)
        {
            // HalaqaSupervisors can only remove from their assigned halaqas
            if (_currentUserService.IsHalaqaSupervisor)
            {
                var canAccess = await _currentUserService.CanAccessHalaqaAsync(halaqaId);
                if (!canAccess)
                    return Forbid();
            }
            
            var success = await _teacherService.RemoveTeacherFromHalaqaAsync(teacherId, halaqaId);
            if (!success)
                return NotFound(new { message = AppConstants.ErrorMessages.AssignmentNotFound });

            return Ok(new { message = AppConstants.SuccessMessages.TeacherRemovedFromHalaqa });
        }
    }
}
