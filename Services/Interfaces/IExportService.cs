namespace KhairAPI.Services.Interfaces
{
    public interface IExportService
    {
        Task<byte[]> ExportStudentsToExcelAsync(IEnumerable<int>? halaqaIds = null, int? teacherId = null);
        Task<byte[]> ExportTeachersToExcelAsync(IEnumerable<int>? halaqaIds = null);
        Task<byte[]> ExportAttendanceReportToExcelAsync(DateTime fromDate, DateTime toDate, IEnumerable<int>? halaqaIds = null, int? teacherId = null);
        Task<byte[]> ExportHalaqaPerformanceToExcelAsync(DateTime fromDate, DateTime toDate, IEnumerable<int>? halaqaIds = null);
        Task<byte[]> ExportTeacherPerformanceToExcelAsync(DateTime fromDate, DateTime toDate, IEnumerable<int>? halaqaIds = null, int? teacherId = null);
        Task<byte[]> ExportTeacherAttendanceReportAsync(DateTime fromDate, DateTime toDate, IEnumerable<int>? halaqaIds = null, int? teacherId = null);
    }
}

