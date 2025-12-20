using KhairAPI.Models.DTOs;

namespace KhairAPI.Services.Interfaces
{
    public interface ITeacherAttendanceService
    {
        Task<TodayTeacherAttendanceResponseDto> GetTodayAttendanceAsync();
        Task<bool> SaveBulkAttendanceAsync(BulkTeacherAttendanceDto dto);
        Task<bool> UpdateAttendanceAsync(int id, UpdateTeacherAttendanceDto dto);
        Task<bool> DeleteAttendanceAsync(int id);
        Task<IEnumerable<TeacherAttendanceRecordDto>> GetTeacherAttendanceHistoryAsync(
            int teacherId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<MonthlyAttendanceReportDto> GetMonthlyReportAsync(int year, int month);
    }
}

