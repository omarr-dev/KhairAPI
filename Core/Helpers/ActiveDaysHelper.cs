namespace KhairAPI.Core.Helpers
{
    /// <summary>
    /// Centralized parsing for a Halaqa's ActiveDays string (e.g. "0,1,3,4" = Sun,Mon,Wed,Thu).
    /// An empty/unset value means the halaqa is active every day (all 7 days).
    /// </summary>
    public static class ActiveDaysHelper
    {
        /// <summary>All days of the week (0 = Sunday … 6 = Saturday).</summary>
        public static readonly IReadOnlyList<int> AllDays = new[] { 0, 1, 2, 3, 4, 5, 6 };

        /// <summary>
        /// Parses an ActiveDays string into the set of active day-of-week numbers (0-6).
        /// Empty or unset => all 7 days.
        /// </summary>
        public static HashSet<int> Parse(string? activeDays)
        {
            if (string.IsNullOrWhiteSpace(activeDays))
                return new HashSet<int>(AllDays);

            return activeDays
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(d => int.TryParse(d.Trim(), out var day) ? day : -1)
                .Where(d => d >= 0 && d <= 6)
                .ToHashSet();
        }

        /// <summary>Returns true if the halaqa is active on the given day-of-week (0-6).</summary>
        public static bool IsActiveOn(string? activeDays, int dayOfWeek) =>
            Parse(activeDays).Contains(dayOfWeek);
    }
}
