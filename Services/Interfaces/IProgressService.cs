using KhairAPI.Models.DTOs;

namespace KhairAPI.Services.Interfaces
{
    public interface IProgressService
    {
        Task<ProgressRecordDto> CreateProgressRecordAsync(CreateProgressRecordDto dto, bool isSupervisorRecording = false);
        Task<IEnumerable<ProgressRecordDto>> GetProgressByDateAsync(DateTime date, int? teacherId = null, List<int>? halaqaFilter = null);
        Task<IEnumerable<ProgressRecordDto>> GetProgressByStudentAsync(int studentId, DateTime? fromDate = null);
        Task<DailyProgressSummaryDto> GetDailyProgressSummaryAsync(DateTime date, int? teacherId = null, List<int>? halaqaFilter = null);
        Task<StudentProgressSummaryDto> GetStudentProgressSummaryAsync(int studentId);
        Task<ProgressRecordDto?> GetLastProgressByTypeAsync(int studentId, int type);
        Task<bool> TeacherHasAccessToStudentAsync(int teacherId, int studentId);
        Task<bool> DeleteProgressRecordAsync(int id, int userId);
    }
}

