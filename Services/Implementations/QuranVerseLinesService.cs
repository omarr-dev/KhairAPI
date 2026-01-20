using System.Collections.Frozen;
using System.Text.Json;
using KhairAPI.Services.Interfaces;

namespace KhairAPI.Services.Implementations
{
    /// <summary>
    /// High-performance service for calculating accurate Quran verse line counts.
    /// 
    /// Performance optimizations:
    /// - FrozenDictionary for O(1) lookups with minimal overhead
    /// - Pre-computed cumulative sums for O(1) range calculations
    /// - Cached normalized surah name lookups
    /// - Thread-safe singleton design
    /// </summary>
    public sealed class QuranVerseLinesService : IQuranVerseLinesService
    {
        // Immutable frozen collections for thread-safe, high-performance lookups
        private readonly FrozenDictionary<string, double> _verseLinesMap;
        private readonly FrozenDictionary<string, int> _surahNameToNumber;
        
        // Pre-computed cumulative sums for O(1) range calculations
        // Key: "surah:verse", Value: cumulative sum of lines from verse 1 to this verse
        private readonly FrozenDictionary<string, double> _cumulativeLines;
        
        private readonly ILogger<QuranVerseLinesService> _logger;
        
        // Constants
        private const double DefaultLinePerVerse = 1.0;
        private const int TotalSurahs = 114;

        public QuranVerseLinesService(ILogger<QuranVerseLinesService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            var verseLinesData = LoadVerseLinesData();
            _verseLinesMap = verseLinesData.ToFrozenDictionary();
            _surahNameToNumber = InitializeSurahMapping().ToFrozenDictionary();
            _cumulativeLines = BuildCumulativeLines(verseLinesData).ToFrozenDictionary();
        }

        #region Public API

        /// <inheritdoc />
        public double CalculateLines(int surahNumber, int fromVerse, int toVerse)
        {
            // Input validation
            if (surahNumber < 1 || surahNumber > TotalSurahs)
            {
                _logger.LogWarning("Invalid surah number {SurahNumber}. Must be between 1 and 114.", surahNumber);
                return DefaultLinePerVerse * (toVerse - fromVerse + 1);
            }
            
            if (fromVerse < 1 || toVerse < fromVerse)
            {
                _logger.LogWarning("Invalid verse range: from {FromVerse} to {ToVerse}.", fromVerse, toVerse);
                return 0;
            }

            // O(1) calculation using pre-computed cumulative sums
            var endKey = $"{surahNumber}:{toVerse}";
            var startKey = $"{surahNumber}:{fromVerse - 1}";
            
            var endCumulative = _cumulativeLines.GetValueOrDefault(endKey, 0);
            var startCumulative = fromVerse > 1 
                ? _cumulativeLines.GetValueOrDefault(startKey, 0) 
                : 0;
            
            var result = endCumulative - startCumulative;
            
            // Fallback if cumulative data is missing
            if (result <= 0 && toVerse >= fromVerse)
            {
                return CalculateLinesIterative(surahNumber, fromVerse, toVerse);
            }
            
            return result;
        }

        /// <inheritdoc />
        public double CalculateLinesBySurahName(string surahName, int fromVerse, int toVerse)
        {
            if (string.IsNullOrWhiteSpace(surahName))
            {
                return DefaultLinePerVerse * (toVerse - fromVerse + 1);
            }
            
            var surahNumber = GetSurahNumber(surahName);
            if (surahNumber == null)
            {
                _logger.LogDebug("Surah name '{SurahName}' not recognized. Using fallback calculation.", surahName);
                return DefaultLinePerVerse * (toVerse - fromVerse + 1);
            }
            
            return CalculateLines(surahNumber.Value, fromVerse, toVerse);
        }

        /// <inheritdoc />
        public double GetVerseLines(int surahNumber, int verseNumber)
        {
            if (surahNumber < 1 || surahNumber > TotalSurahs || verseNumber < 1)
            {
                return DefaultLinePerVerse;
            }
            
            var key = $"{surahNumber}:{verseNumber}";
            return _verseLinesMap.GetValueOrDefault(key, DefaultLinePerVerse);
        }

        /// <inheritdoc />
        public int? GetSurahNumber(string surahName)
        {
            if (string.IsNullOrWhiteSpace(surahName))
                return null;

            var trimmed = surahName.Trim();
            
            // Fast path: exact match
            if (_surahNameToNumber.TryGetValue(trimmed, out var number))
                return number;

            // Normalize: remove "سورة" prefix variants
            var normalized = NormalizeSurahName(trimmed);
            if (_surahNameToNumber.TryGetValue(normalized, out number))
                return number;

            return null;
        }

        #endregion

        #region Private Methods

        private static string NormalizeSurahName(string name)
        {
            // Remove common prefixes
            var result = name
                .Replace("سورة ", string.Empty)
                .Replace("سورة", string.Empty)
                .Trim();
            
            return result;
        }

        private double CalculateLinesIterative(int surahNumber, int fromVerse, int toVerse)
        {
            double total = 0;
            for (int verse = fromVerse; verse <= toVerse; verse++)
            {
                total += GetVerseLines(surahNumber, verse);
            }
            return total;
        }

        private Dictionary<string, double> BuildCumulativeLines(Dictionary<string, double> verseLinesData)
        {
            var cumulative = new Dictionary<string, double>(verseLinesData.Count);
            
            for (int surah = 1; surah <= TotalSurahs; surah++)
            {
                double runningSum = 0;
                int verse = 1;
                
                while (true)
                {
                    var key = $"{surah}:{verse}";
                    if (!verseLinesData.TryGetValue(key, out var lines))
                        break; // No more verses in this surah
                    
                    runningSum += lines;
                    cumulative[key] = runningSum;
                    verse++;
                }
            }
            
            _logger.LogDebug("Built cumulative line sums for {Count} verses.", cumulative.Count);
            return cumulative;
        }

        private Dictionary<string, double> LoadVerseLinesData()
        {
            try
            {
                var basePath = AppDomain.CurrentDomain.BaseDirectory;
                var jsonPath = Path.Combine(basePath, "Data", "quran-verse-lines.json");
                
                if (!File.Exists(jsonPath))
                {
                    jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "quran-verse-lines.json");
                }
                
                if (!File.Exists(jsonPath))
                {
                    _logger.LogWarning(
                        "Quran verse lines data file not found. Using fallback calculation (1 verse = 1 line). " +
                        "Expected path: {Path}", jsonPath);
                    return new Dictionary<string, double>();
                }

                using var stream = File.OpenRead(jsonPath);
                var data = JsonSerializer.Deserialize<Dictionary<string, double>>(stream);
                
                if (data == null || data.Count == 0)
                {
                    _logger.LogWarning("Quran verse lines data is empty. Using fallback calculation.");
                    return new Dictionary<string, double>();
                }

                _logger.LogInformation(
                    "Loaded {Count} verse line mappings from Quran data file.", 
                    data.Count);
                return data;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse Quran verse lines JSON. Using fallback calculation.");
                return new Dictionary<string, double>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Quran verse lines data. Using fallback calculation.");
                return new Dictionary<string, double>();
            }
        }

        private static Dictionary<string, int> InitializeSurahMapping()
        {
            // Using capacity hint for better performance
            return new Dictionary<string, int>(140)
            {
                // Standard Arabic names (with ال where appropriate)
                { "الفاتحة", 1 }, { "البقرة", 2 }, { "آل عمران", 3 }, { "النساء", 4 },
                { "المائدة", 5 }, { "الأنعام", 6 }, { "الأعراف", 7 }, { "الأنفال", 8 },
                { "التوبة", 9 }, { "يونس", 10 }, { "هود", 11 }, { "يوسف", 12 },
                { "الرعد", 13 }, { "إبراهيم", 14 }, { "الحجر", 15 }, { "النحل", 16 },
                { "الإسراء", 17 }, { "الكهف", 18 }, { "مريم", 19 }, { "طه", 20 },
                { "الأنبياء", 21 }, { "الحج", 22 }, { "المؤمنون", 23 }, { "النور", 24 },
                { "الفرقان", 25 }, { "الشعراء", 26 }, { "النمل", 27 }, { "القصص", 28 },
                { "العنكبوت", 29 }, { "الروم", 30 }, { "لقمان", 31 }, { "السجدة", 32 },
                { "الأحزاب", 33 }, { "سبأ", 34 }, { "فاطر", 35 }, { "يس", 36 },
                { "الصافات", 37 }, { "ص", 38 }, { "الزمر", 39 }, { "غافر", 40 },
                { "فصلت", 41 }, { "الشورى", 42 }, { "الزخرف", 43 }, { "الدخان", 44 },
                { "الجاثية", 45 }, { "الأحقاف", 46 }, { "محمد", 47 }, { "الفتح", 48 },
                { "الحجرات", 49 }, { "ق", 50 }, { "الذاريات", 51 }, { "الطور", 52 },
                { "النجم", 53 }, { "القمر", 54 }, { "الرحمن", 55 }, { "الواقعة", 56 },
                { "الحديد", 57 }, { "المجادلة", 58 }, { "الحشر", 59 }, { "الممتحنة", 60 },
                { "الصف", 61 }, { "الجمعة", 62 }, { "المنافقون", 63 }, { "التغابن", 64 },
                { "الطلاق", 65 }, { "التحريم", 66 }, { "الملك", 67 }, { "القلم", 68 },
                { "الحاقة", 69 }, { "المعارج", 70 }, { "نوح", 71 }, { "الجن", 72 },
                { "المزمل", 73 }, { "المدثر", 74 }, { "القيامة", 75 }, { "الإنسان", 76 },
                { "المرسلات", 77 }, { "النبأ", 78 }, { "النازعات", 79 }, { "عبس", 80 },
                { "التكوير", 81 }, { "الانفطار", 82 }, { "المطففين", 83 }, { "الانشقاق", 84 },
                { "البروج", 85 }, { "الطارق", 86 }, { "الأعلى", 87 }, { "الغاشية", 88 },
                { "الفجر", 89 }, { "البلد", 90 }, { "الشمس", 91 }, { "الليل", 92 },
                { "الضحى", 93 }, { "الشرح", 94 }, { "التين", 95 }, { "العلق", 96 },
                { "القدر", 97 }, { "البينة", 98 }, { "الزلزلة", 99 }, { "العاديات", 100 },
                { "القارعة", 101 }, { "التكاثر", 102 }, { "العصر", 103 }, { "الهمزة", 104 },
                { "الفيل", 105 }, { "قريش", 106 }, { "الماعون", 107 }, { "الكوثر", 108 },
                { "الكافرون", 109 }, { "النصر", 110 }, { "المسد", 111 }, { "الإخلاص", 112 },
                { "الفلق", 113 }, { "الناس", 114 },
                
                // Alternative names without ال
                { "فاتحة", 1 }, { "بقرة", 2 }, { "نساء", 4 }, { "مائدة", 5 },
                { "أنعام", 6 }, { "أعراف", 7 }, { "أنفال", 8 }, { "توبة", 9 },
                
                // Common alternative names
                { "الاسراء", 17 }, { "الملك", 67 }, { "تبارك", 67 },
            };
        }

        #endregion
    }
}
