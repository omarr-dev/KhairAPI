using KhairAPI.Models.DTOs;

namespace KhairAPI.Services.Interfaces
{
    /// <summary>
    /// Service for managing student daily targets and achievement tracking.
    /// Achievements are calculated on-demand from ProgressRecords - no background job needed.
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
        /// Gets achievement history for a student within a date range.
        /// Includes daily achievements, streak information, and summary statistics.
        /// For single-day queries, use the same date for start and end.
        /// </summary>
        /// <param name="studentId">The student ID</param>
        /// <param name="startDate">Start date of the range (inclusive)</param>
        /// <param name="endDate">End date of the range (inclusive)</param>
        /// <returns>Achievement history with streak info, or null if no target set</returns>
        Task<AchievementHistoryDto?> GetAchievementHistoryAsync(int studentId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Gets achievement history for multiple students in a single batch call.
        /// Optimized for the "My Students" page to show streaks for all students.
        /// </summary>
        /// <param name="studentIds">List of student IDs</param>
        /// <param name="startDate">Start date of the range (inclusive)</param>
        /// <param name="endDate">End date of the range (inclusive)</param>
        /// <returns>Dictionary mapping student ID to their achievement history</returns>
        Task<Dictionary<int, AchievementHistoryDto>> GetAchievementHistoryBatchAsync(
            IEnumerable<int> studentIds, 
            DateTime startDate, 
            DateTime endDate);
    }
}
