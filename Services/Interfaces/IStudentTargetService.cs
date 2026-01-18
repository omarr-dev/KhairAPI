using KhairAPI.Models.DTOs;

namespace KhairAPI.Services.Interfaces
{
    /// <summary>
    /// Service for managing student daily targets and tracking achievement history.
    /// </summary>
    public interface IStudentTargetService
    {
        /// <summary>
        /// Gets the current target for a student.
        /// </summary>
        Task<StudentTargetDto?> GetTargetAsync(int studentId);

        /// <summary>
        /// Sets or updates the target for a single student.
        /// </summary>
        Task<StudentTargetDto> SetTargetAsync(int studentId, SetStudentTargetDto dto);

        /// <summary>
        /// Bulk sets targets for multiple students based on criteria.
        /// Returns the number of students updated.
        /// </summary>
        Task<int> BulkSetTargetAsync(BulkSetTargetDto dto);

        /// <summary>
        /// Gets target achievement history for a student.
        /// </summary>
        Task<List<TargetAchievementDto>> GetAchievementHistoryAsync(int studentId, AchievementHistoryFilter? filter = null);

        /// <summary>
        /// Records daily achievements for all students. Should be called by a background job at end of day.
        /// </summary>
        Task RecordDailyAchievementsAsync();
    }
}
