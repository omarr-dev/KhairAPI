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
    public class HalaqatController : ControllerBase
    {
        private readonly IHalaqaService _halaqaService;
        private readonly ICurrentUserService _currentUserService;

        public HalaqatController(IHalaqaService halaqaService, ICurrentUserService currentUserService)
        {
            _halaqaService = halaqaService;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllHalaqat()
        {
            int? teacherId = null;
            
            if (_currentUserService.IsTeacher)
            {
                teacherId = await _currentUserService.GetTeacherIdAsync();
                if (!teacherId.HasValue)
                    return Unauthorized(new { message = AppConstants.ErrorMessages.CannotIdentifyTeacher });
            }

            var halaqat = await _halaqaService.GetAllHalaqatAsync(teacherId);
            return Ok(halaqat);
        }

        [HttpGet("hierarchy")]
        [Authorize(Policy = "SupervisorOnly")]
        public async Task<IActionResult> GetHalaqatHierarchy()
        {
            var hierarchy = await _halaqaService.GetHalaqatHierarchyAsync();
            return Ok(hierarchy);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetHalaqaById(int id)
        {
            var halaqa = await _halaqaService.GetHalaqaByIdAsync(id);
            if (halaqa == null)
                return NotFound(new { message = AppConstants.ErrorMessages.HalaqaNotFound });

            return Ok(halaqa);
        }

        [HttpPost]
        [Authorize(Policy = "SupervisorOnly")]
        public async Task<IActionResult> CreateHalaqa([FromBody] CreateHalaqaDto dto)
        {
            var halaqa = await _halaqaService.CreateHalaqaAsync(dto);
            return CreatedAtAction(nameof(GetHalaqaById), new { id = halaqa.Id }, halaqa);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "SupervisorOnly")]
        public async Task<IActionResult> UpdateHalaqa(int id, [FromBody] UpdateHalaqaDto dto)
        {
            var success = await _halaqaService.UpdateHalaqaAsync(id, dto);
            if (!success)
                return NotFound(new { message = AppConstants.ErrorMessages.HalaqaNotFound });

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "SupervisorOnly")]
        public async Task<IActionResult> DeleteHalaqa(int id)
        {
            var success = await _halaqaService.DeleteHalaqaAsync(id);
            if (!success)
                return NotFound(new { message = AppConstants.ErrorMessages.HalaqaNotFound });

            return NoContent();
        }
    }
}
