namespace KhairAPI.Services.Interfaces
{
    public interface IAttendanceBackgroundService
    {
        /// <summary>
        /// Marks students as absent for a specific date if they don't have attendance records
        /// and it was an active day for their Halaqa.
        /// Runs automatically at 23:59 each day.
        /// </summary>
        /// <param name="date">The date to process. If null, uses the current date.</param>
        /// <returns>Number of absent records created</returns>
        Task<int> MarkAbsentForMissingAttendanceAsync(DateTime? date = null);

        /// <summary>
        /// Resets streaks for students who didn't meet their target on an active halaqa day.
        /// Should run daily at end of day (23:59).
        /// </summary>
        /// <param name="date">The date to check. If null, uses the current date.</param>
        /// <returns>Number of streaks reset</returns>
        Task<int> ResetStreaksForMissedTargetsAsync(DateTime? date = null);
    }
}

