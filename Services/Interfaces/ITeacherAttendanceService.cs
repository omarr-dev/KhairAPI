using KhairAPI.Models.DTOs;

namespace KhairAPI.Services.Interfaces
{
    public interface ITeacherAttendanceService
    {
        Task<TodayTeacherAttendanceResponseDto> GetTodayAttendanceAsync(List<int>? halaqaFilter = null);
        Task<bool> SaveBulkAttendanceAsync(BulkTeacherAttendanceDto dto);
        Task<bool> UpdateAttendanceAsync(int id, UpdateTeacherAttendanceDto dto);
        Task<bool> DeleteAttendanceAsync(int id);
        Task<IEnumerable<TeacherAttendanceRecordDto>> GetTeacherAttendanceHistoryAsync(
            int teacherId, DateTime? fromDate = null, DateTime? toDate = null, List<int>? halaqaFilter = null);
        Task<MonthlyAttendanceReportDto> GetMonthlyReportAsync(int year, int month, List<int>? halaqaFilter = null);

        /// <summary>
        /// Gets a teacher's own attendance status for today (for the self check-in button).
        /// </summary>
        Task<TeacherSelfAttendanceStatusDto> GetSelfAttendanceStatusAsync(int teacherId);

        /// <summary>
        /// Marks the teacher present in all of their halaqat that are active today.
        /// Existing records (e.g. already set by a supervisor) are left untouched.
        /// </summary>
        Task<TeacherSelfCheckInResultDto> SelfCheckInAsync(int teacherId);

        /// <summary>
        /// Records the teacher's departure time on today's present records.
        /// Requires the teacher to be checked in first.
        /// </summary>
        Task<TeacherSelfCheckInResultDto> SelfCheckOutAsync(int teacherId);
    }
}


