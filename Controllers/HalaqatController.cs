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
    public class HalaqatController : ControllerBase
    {
        private readonly IHalaqaService _halaqaService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IHalaqaSupervisorService _halaqaSupervisorService;

        public HalaqatController(
            IHalaqaService halaqaService, 
            ICurrentUserService currentUserService,
            IHalaqaSupervisorService halaqaSupervisorService)
        {
            _halaqaService = halaqaService;
            _currentUserService = currentUserService;
            _halaqaSupervisorService = halaqaSupervisorService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllHalaqat()
        {
            int? teacherId = null;
            List<int>? supervisedHalaqaIds = null;
            
            if (_currentUserService.IsTeacher)
            {
                teacherId = await _currentUserService.GetTeacherIdAsync();
                if (!teacherId.HasValue)
                    return Unauthorized(new { message = AppConstants.ErrorMessages.CannotIdentifyTeacher });
            }
            else if (_currentUserService.IsHalaqaSupervisor)
            {
                // HalaqaSupervisors only see their assigned halaqas
                supervisedHalaqaIds = await _currentUserService.GetSupervisedHalaqaIdsAsync();
            }
            // Full Supervisors see all halaqas (no filtering)

            var halaqat = await _halaqaService.GetAllHalaqatAsync(teacherId, supervisedHalaqaIds);
            return Ok(halaqat);
        }

        [HttpGet("hierarchy")]
        [Authorize(Policy = AppConstants.Policies.HalaqaSupervisorOrHigher)]
        public async Task<IActionResult> GetHalaqatHierarchy()
        {
            // HalaqaSupervisors get filtered hierarchy
            if (_currentUserService.IsHalaqaSupervisor)
            {
                var supervisedHalaqaIds = await _currentUserService.GetSupervisedHalaqaIdsAsync();
                var hierarchy = await _halaqaService.GetHalaqatHierarchyAsync(supervisedHalaqaIds);
                return Ok(hierarchy);
            }

            // Full Supervisors get complete hierarchy
            var fullHierarchy = await _halaqaService.GetHalaqatHierarchyAsync();
            return Ok(fullHierarchy);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetHalaqaById(int id)
        {
            // Check access for HalaqaSupervisors
            if (_currentUserService.IsHalaqaSupervisor)
            {
                var canAccess = await _currentUserService.CanAccessHalaqaAsync(id);
                if (!canAccess)
                    return Forbid();
            }

            var halaqa = await _halaqaService.GetHalaqaByIdAsync(id);
            if (halaqa == null)
                return NotFound(new { message = AppConstants.ErrorMessages.HalaqaNotFound });

            return Ok(halaqa);
        }

        [HttpPost]
        [Authorize(Policy = AppConstants.Policies.SupervisorOnly)]
        public async Task<IActionResult> CreateHalaqa([FromBody] CreateHalaqaDto dto)
        {
            var halaqa = await _halaqaService.CreateHalaqaAsync(dto);
            return CreatedAtAction(nameof(GetHalaqaById), new { id = halaqa.Id }, halaqa);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = AppConstants.Policies.HalaqaSupervisorOrHigher)]
        public async Task<IActionResult> UpdateHalaqa(int id, [FromBody] UpdateHalaqaDto dto)
        {
            // HalaqaSupervisors can only update their assigned halaqas
            if (_currentUserService.IsHalaqaSupervisor)
            {
                var canAccess = await _currentUserService.CanAccessHalaqaAsync(id);
                if (!canAccess)
                    return Forbid();
            }

            var success = await _halaqaService.UpdateHalaqaAsync(id, dto);
            if (!success)
                return NotFound(new { message = AppConstants.ErrorMessages.HalaqaNotFound });

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = AppConstants.Policies.SupervisorOnly)]
        public async Task<IActionResult> DeleteHalaqa(int id)
        {
            var success = await _halaqaService.DeleteHalaqaAsync(id);
            if (!success)
                return NotFound(new { message = AppConstants.ErrorMessages.HalaqaNotFound });

            return NoContent();
        }

        #region HalaqaSupervisor Assignment Management (Supervisor Only)

        /// <summary>
        /// Get all HalaqaSupervisor users in the association
        /// </summary>
        [HttpGet("supervisors")]
        [Authorize(Policy = AppConstants.Policies.SupervisorOnly)]
        public async Task<IActionResult> GetAllHalaqaSupervisors()
        {
            var supervisors = await _halaqaSupervisorService.GetAllHalaqaSupervisorsAsync();
            return Ok(supervisors);
        }

        /// <summary>
        /// Get HalaqaSupervisors assigned to a specific halaqa
        /// </summary>
        [HttpGet("{halaqaId}/supervisors")]
        [Authorize(Policy = AppConstants.Policies.SupervisorOnly)]
        public async Task<IActionResult> GetSupervisorsForHalaqa(int halaqaId)
        {
            var supervisors = await _halaqaSupervisorService.GetSupervisorsForHalaqaAsync(halaqaId);
            return Ok(supervisors);
        }

        /// <summary>
        /// Assign a HalaqaSupervisor to a halaqa
        /// </summary>
        [HttpPost("{halaqaId}/supervisors/{userId}")]
        [Authorize(Policy = AppConstants.Policies.SupervisorOnly)]
        public async Task<IActionResult> AssignSupervisorToHalaqa(int halaqaId, int userId)
        {
            try
            {
                var assignment = await _halaqaSupervisorService.AssignToHalaqaAsync(userId, halaqaId);
                return Ok(assignment);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Remove a HalaqaSupervisor from a halaqa
        /// </summary>
        [HttpDelete("{halaqaId}/supervisors/{userId}")]
        [Authorize(Policy = AppConstants.Policies.SupervisorOnly)]
        public async Task<IActionResult> RemoveSupervisorFromHalaqa(int halaqaId, int userId)
        {
            var success = await _halaqaSupervisorService.RemoveFromHalaqaAsync(userId, halaqaId);
            if (!success)
                return NotFound(new { message = "التعيين غير موجود" });

            return Ok(new { message = "تم إزالة مشرف الحلقة بنجاح" });
        }

        #endregion
    }
}
