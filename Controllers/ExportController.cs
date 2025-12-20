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

        public ExportController(IExportService exportService)
        {
            _exportService = exportService;
        }

        [HttpGet("students")]
        [Authorize(Policy = "SupervisorOnly")]
        public async Task<IActionResult> ExportStudents([FromQuery] int? halaqaId = null, [FromQuery] int? teacherId = null)
        {
            var bytes = await _exportService.ExportStudentsToExcelAsync(halaqaId, teacherId);
            var fileName = $"students_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet("teachers")]
        [Authorize(Policy = "SupervisorOnly")]
        public async Task<IActionResult> ExportTeachers([FromQuery] int? halaqaId = null)
        {
            var bytes = await _exportService.ExportTeachersToExcelAsync(halaqaId);
            var fileName = $"teachers_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet("attendance")]
        [Authorize(Policy = "SupervisorOnly")]
        public async Task<IActionResult> ExportAttendance(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int? halaqaId = null)
        {
            var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
            var to = toDate ?? DateTime.UtcNow;

            var bytes = await _exportService.ExportAttendanceReportToExcelAsync(from, to, halaqaId);
            var fileName = $"attendance_report_{from:yyyyMMdd}_to_{to:yyyyMMdd}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet("halaqa-performance")]
        [Authorize(Policy = "SupervisorOnly")]
        public async Task<IActionResult> ExportHalaqaPerformance(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
            var to = toDate ?? DateTime.UtcNow;

            var bytes = await _exportService.ExportHalaqaPerformanceToExcelAsync(from, to);
            var fileName = $"halaqa_performance_{from:yyyyMMdd}_to_{to:yyyyMMdd}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet("teacher-performance")]
        [Authorize(Policy = "SupervisorOnly")]
        public async Task<IActionResult> ExportTeacherPerformance(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
            var to = toDate ?? DateTime.UtcNow;

            var bytes = await _exportService.ExportTeacherPerformanceToExcelAsync(from, to);
            var fileName = $"teacher_performance_{from:yyyyMMdd}_to_{to:yyyyMMdd}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet("teacher-attendance")]
        [Authorize(Policy = "SupervisorOnly")]
        public async Task<IActionResult> ExportTeacherAttendance(
            [FromQuery] int? year = null,
            [FromQuery] int? month = null)
        {
            var targetYear = year ?? DateTime.UtcNow.Year;
            var targetMonth = month ?? DateTime.UtcNow.Month;

            if (targetMonth < 1 || targetMonth > 12)
                return BadRequest(new { message = AppConstants.ErrorMessages.InvalidMonth });

            if (targetYear < 2020 || targetYear > 2100)
                return BadRequest(new { message = AppConstants.ErrorMessages.InvalidYear });

            var bytes = await _exportService.ExportTeacherAttendanceReportAsync(targetYear, targetMonth);
            var fileName = $"teacher_attendance_{targetYear}_{targetMonth:D2}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
