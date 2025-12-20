using KhairAPI.Models.DTOs;
using KhairAPI.Models.Entities;

namespace KhairAPI.Services.Interfaces
{
    public interface IAttendanceService
    {
        Task<AttendanceRecordDto> CreateAttendanceAsync(CreateAttendanceDto dto);
        Task<bool> CreateBulkAttendanceAsync(BulkAttendanceDto dto);
        Task<AttendanceSummaryDto> GetAttendanceByDateAsync(int halaqaId, DateTime date);
        Task<IEnumerable<AttendanceRecordDto>> GetStudentAttendanceAsync(int studentId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<StudentAttendanceSummaryDto> GetStudentAttendanceSummaryAsync(int studentId, DateTime fromDate, DateTime toDate);
        Task<bool> UpdateAttendanceAsync(int id, AttendanceStatus status, string? notes);
        Task<bool> DeleteAttendanceAsync(int id);
    }
}

