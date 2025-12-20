namespace KhairAPI.Services.Interfaces
{
    public interface IAttendanceBackgroundService
    {
        /// <summary>
        /// Marks students as absent for a specific date if they don't have attendance records
        /// and it was an active day for their Halaqa.
        /// Runs automatically at 23:59 each day.
        /// </summary>
        /// <param name="date">The date to process</param>
        /// <returns>Number of absent records created</returns>
        Task<int> MarkAbsentForMissingAttendanceAsync(DateTime date);
    }
}

