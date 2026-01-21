using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KhairAPI.Services.Interfaces;
using KhairAPI.Core.Helpers;

namespace KhairAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ExportController : ControllerBase
    {
        private readonly IExportService _exportService;
        private readonly ICurrentUserService _currentUserService;

        public ExportController(IExportService exportService, ICurrentUserService currentUserService)
        {
            _exportService = exportService;
            _currentUserService = currentUserService;
        }

        private async Task<IEnumerable<int>?> GetEffectiveHalaqaIds(int? requestedHalaqaId)
        {
            var supervisedHalaqaIds = await _currentUserService.GetSupervisedHalaqaIdsAsync();
            
            // Full supervisor
            if (supervisedHalaqaIds == null)
            {
                return requestedHalaqaId.HasValue ? new[] { requestedHalaqaId.Value } : null;
            }
            
            // HalaqaSupervisor
            if (requestedHalaqaId.HasValue)
            {
                // If they requested a specific halaqa, check if they have access to it
                return supervisedHalaqaIds.Contains(requestedHalaqaId.Value) 
                    ? new[] { requestedHalaqaId.Value } 
                    : new int[0]; // No access, return empty list to filter everything out
            }
            
            // Return all supervised halaqas
            return supervisedHalaqaIds;
        }

        [HttpGet("students")]
        [Authorize(Policy = AppConstants.Policies.HalaqaSupervisorOrHigher)]
        public async Task<IActionResult> ExportStudents([FromQuery] int? halaqaId = null, [FromQuery] int? teacherId = null)
        {
            var halaqaIds = await GetEffectiveHalaqaIds(halaqaId);
            var bytes = await _exportService.ExportStudentsToExcelAsync(halaqaIds, teacherId);
            var fileName = $"students_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet("teachers")]
        [Authorize(Policy = AppConstants.Policies.HalaqaSupervisorOrHigher)]
        public async Task<IActionResult> ExportTeachers([FromQuery] int? halaqaId = null)
        {
            var halaqaIds = await GetEffectiveHalaqaIds(halaqaId);
            var bytes = await _exportService.ExportTeachersToExcelAsync(halaqaIds);
            var fileName = $"teachers_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet("attendance")]
        [Authorize(Policy = AppConstants.Policies.HalaqaSupervisorOrHigher)]
        public async Task<IActionResult> ExportAttendance(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int? halaqaId = null)
        {
            var from = fromDate.HasValue ? DateTime.SpecifyKind(fromDate.Value, DateTimeKind.Utc) : DateTime.UtcNow.AddDays(-30);
            var to = toDate.HasValue ? DateTime.SpecifyKind(toDate.Value, DateTimeKind.Utc) : DateTime.UtcNow;
            
            var halaqaIds = await GetEffectiveHalaqaIds(halaqaId);
            var bytes = await _exportService.ExportAttendanceReportToExcelAsync(from, to, halaqaIds);
            var fileName = $"attendance_report_{from:yyyyMMdd}_to_{to:yyyyMMdd}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet("halaqa-performance")]
        [Authorize(Policy = AppConstants.Policies.HalaqaSupervisorOrHigher)]
        public async Task<IActionResult> ExportHalaqaPerformance(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int? halaqaId = null)
        {
            var from = fromDate.HasValue ? DateTime.SpecifyKind(fromDate.Value, DateTimeKind.Utc) : DateTime.UtcNow.AddDays(-30);
            var to = toDate.HasValue ? DateTime.SpecifyKind(toDate.Value, DateTimeKind.Utc) : DateTime.UtcNow;

            var halaqaIds = await GetEffectiveHalaqaIds(halaqaId);
            var bytes = await _exportService.ExportHalaqaPerformanceToExcelAsync(from, to, halaqaIds);
            var fileName = $"halaqa_performance_{from:yyyyMMdd}_to_{to:yyyyMMdd}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet("teacher-performance")]
        [Authorize(Policy = AppConstants.Policies.HalaqaSupervisorOrHigher)]
        public async Task<IActionResult> ExportTeacherPerformance(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int? halaqaId = null)
        {
            var from = fromDate.HasValue ? DateTime.SpecifyKind(fromDate.Value, DateTimeKind.Utc) : DateTime.UtcNow.AddDays(-30);
            var to = toDate.HasValue ? DateTime.SpecifyKind(toDate.Value, DateTimeKind.Utc) : DateTime.UtcNow;

            var halaqaIds = await GetEffectiveHalaqaIds(halaqaId);
            var bytes = await _exportService.ExportTeacherPerformanceToExcelAsync(from, to, halaqaIds);
            var fileName = $"teacher_performance_{from:yyyyMMdd}_to_{to:yyyyMMdd}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet("teacher-attendance")]
        [Authorize(Policy = AppConstants.Policies.HalaqaSupervisorOrHigher)]
        public async Task<IActionResult> ExportTeacherAttendance(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int? halaqaId = null)
        {
            var from = fromDate.HasValue ? DateTime.SpecifyKind(fromDate.Value, DateTimeKind.Utc) : DateTime.UtcNow.AddDays(-30);
            var to = toDate.HasValue ? DateTime.SpecifyKind(toDate.Value, DateTimeKind.Utc) : DateTime.UtcNow;

            var halaqaIds = await GetEffectiveHalaqaIds(halaqaId);
            var bytes = await _exportService.ExportTeacherAttendanceReportAsync(from, to, halaqaIds);
            var fileName = $"teacher_attendance_{from:yyyyMMdd}_to_{to:yyyyMMdd}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
