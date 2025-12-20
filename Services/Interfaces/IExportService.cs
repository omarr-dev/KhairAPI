namespace KhairAPI.Services.Interfaces
{
    public interface IExportService
    {
        Task<byte[]> ExportStudentsToExcelAsync(int? halaqaId = null, int? teacherId = null);
        Task<byte[]> ExportTeachersToExcelAsync(int? halaqaId = null);
        Task<byte[]> ExportAttendanceReportToExcelAsync(DateTime fromDate, DateTime toDate, int? halaqaId = null);
        Task<byte[]> ExportHalaqaPerformanceToExcelAsync(DateTime fromDate, DateTime toDate);
        Task<byte[]> ExportTeacherPerformanceToExcelAsync(DateTime fromDate, DateTime toDate);
        Task<byte[]> ExportTeacherAttendanceReportAsync(int year, int month);
    }
}

