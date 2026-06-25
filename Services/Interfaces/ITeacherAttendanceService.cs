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
        /// Marks the teacher present in a single halaqa that is active today.
        /// An existing record (e.g. already set by a supervisor) is left untouched.
        /// </summary>
        Task<TeacherSelfCheckInResultDto> SelfCheckInAsync(int teacherId, int halaqaId);

        /// <summary>
        /// Records the teacher's departure time on today's present record for a single halaqa.
        /// Requires the teacher to be checked in to that halaqa first.
        /// </summary>
        Task<TeacherSelfCheckInResultDto> SelfCheckOutAsync(int teacherId, int halaqaId);
    }
}


