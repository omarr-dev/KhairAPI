namespace KhairAPI.Services
{
    /// <summary>
    /// Service for calculating accurate line counts for Quran verses.
    /// Uses the Medina Mushaf verse-to-line mapping data.
    /// </summary>
    public interface IQuranVerseLinesService
    {
        /// <summary>
        /// Calculates the total line count for a range of verses.
        /// </summary>
        /// <param name="surahNumber">The surah number (1-114)</param>
        /// <param name="fromVerse">The starting verse number</param>
        /// <param name="toVerse">The ending verse number</param>
        /// <returns>The total line count (can be fractional)</returns>
        double CalculateLines(int surahNumber, int fromVerse, int toVerse);
        
        /// <summary>
        /// Calculates lines for a verse range using surah name instead of number.
        /// </summary>
        /// <param name="surahName">The Arabic surah name</param>
        /// <param name="fromVerse">The starting verse number</param>
        /// <param name="toVerse">The ending verse number</param>
        /// <returns>The total line count</returns>
        double CalculateLinesBySurahName(string surahName, int fromVerse, int toVerse);
        
        /// <summary>
        /// Gets the line count for a single verse.
        /// </summary>
        double GetVerseLines(int surahNumber, int verseNumber);
        
        /// <summary>
        /// Gets the surah number from its Arabic name.
        /// Returns null if the surah name is not recognized.
        /// </summary>
        int? GetSurahNumber(string surahName);
    }
}
