using KhairAPI.Models.Entities;

namespace KhairAPI.Services.Interfaces
{
    public interface IQuranService
    {
        SurahInfo? GetSurahByNumber(int number);
        SurahInfo? GetSurahByName(string name);
        IEnumerable<SurahInfo> GetAllSurahs();
        decimal CalculateJuzMemorized(MemorizationDirection direction, int currentSurahNumber, int currentVerse);
        (int nextSurah, int nextVerse) GetNextPosition(MemorizationDirection direction, int surahNumber, int toVerse);
    }

    public class SurahInfo
    {
        public int Number { get; }
        public string Name { get; }
        public int VerseCount { get; }
        public int JuzNumber { get; }
        public decimal JuzPortion { get; }

        public SurahInfo(int number, string name, int verseCount, int juzNumber, decimal juzPortion)
        {
            Number = number;
            Name = name;
            VerseCount = verseCount;
            JuzNumber = juzNumber;
            JuzPortion = juzPortion;
        }
    }
}

