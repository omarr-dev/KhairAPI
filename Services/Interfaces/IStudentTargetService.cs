using KhairAPI.Models.DTOs;

namespace KhairAPI.Services.Interfaces
{
    /// <summary>
    /// Service for managing student daily targets.
    /// Simplified: Targets are stored, achievements are calculated on-demand from ProgressRecords.
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
        /// Calculates achievement for a student on a specific date (on-demand).
        /// No background job needed - calculates from ProgressRecords.
        /// </summary>
        Task<TargetAchievementDto?> CalculateAchievementAsync(int studentId, DateTime date);
    }
}
