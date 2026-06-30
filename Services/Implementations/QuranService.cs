using KhairAPI.Models.Entities;
using KhairAPI.Services.Interfaces;

namespace KhairAPI.Services.Implementations
{
    public class QuranService : IQuranService
    {
        private static readonly List<SurahInfo> _surahs = new()
        {
            new SurahInfo(1, "الفاتحة", 7, 1, 0.047m),
            new SurahInfo(2, "البقرة", 286, 1, 2.223m),
            new SurahInfo(3, "آل عمران", 200, 3, 1.555m),
            new SurahInfo(4, "النساء", 176, 4, 1.439m),
            new SurahInfo(5, "المائدة", 120, 6, 0.998m),
            new SurahInfo(6, "الأنعام", 165, 7, 1.126m),
            new SurahInfo(7, "الأعراف", 206, 8, 1.361m),
            new SurahInfo(8, "الأنفال", 75, 9, 0.527m),
            new SurahInfo(9, "التوبة", 129, 10, 0.969m),
            new SurahInfo(10, "يونس", 109, 11, 0.722m),
            new SurahInfo(11, "هود", 123, 11, 0.727m),
            new SurahInfo(12, "يوسف", 111, 12, 0.689m),
            new SurahInfo(13, "الرعد", 43, 13, 0.279m),
            new SurahInfo(14, "إبراهيم", 52, 13, 0.338m),
            new SurahInfo(15, "الحجر", 99, 14, 0.436m),
            new SurahInfo(16, "النحل", 128, 14, 0.564m),
            new SurahInfo(17, "الإسراء", 111, 15, 0.600m),
            new SurahInfo(18, "الكهف", 110, 15, 0.534m),
            new SurahInfo(19, "مريم", 98, 16, 0.364m),
            new SurahInfo(20, "طه", 135, 16, 0.502m),
            new SurahInfo(21, "الأنبياء", 112, 17, 0.589m),
            new SurahInfo(22, "الحج", 78, 17, 0.411m),
            new SurahInfo(23, "المؤمنون", 118, 18, 0.584m),
            new SurahInfo(24, "النور", 64, 18, 0.317m),
            new SurahInfo(25, "الفرقان", 77, 18, 0.267m),
            new SurahInfo(26, "الشعراء", 227, 19, 0.670m),
            new SurahInfo(27, "النمل", 93, 19, 0.384m),
            new SurahInfo(28, "القصص", 88, 20, 0.515m),
            new SurahInfo(29, "العنكبوت", 69, 20, 0.398m),
            new SurahInfo(30, "الروم", 60, 21, 0.337m),
            new SurahInfo(31, "لقمان", 34, 21, 0.191m),
            new SurahInfo(32, "السجدة", 30, 21, 0.169m),
            new SurahInfo(33, "الأحزاب", 73, 21, 0.423m),
            new SurahInfo(34, "سبأ", 54, 22, 0.320m),
            new SurahInfo(35, "فاطر", 45, 22, 0.266m),
            new SurahInfo(36, "يس", 83, 22, 0.317m),
            new SurahInfo(37, "الصافات", 182, 23, 0.510m),
            new SurahInfo(38, "ص", 88, 23, 0.246m),
            new SurahInfo(39, "الزمر", 75, 23, 0.338m),
            new SurahInfo(40, "غافر", 85, 24, 0.486m),
            new SurahInfo(41, "فصلت", 54, 24, 0.295m),
            new SurahInfo(42, "الشورى", 53, 25, 0.215m),
            new SurahInfo(43, "الزخرف", 89, 25, 0.362m),
            new SurahInfo(44, "الدخان", 59, 25, 0.240m),
            new SurahInfo(45, "الجاثية", 37, 25, 0.150m),
            new SurahInfo(46, "الأحقاف", 35, 26, 0.179m),
            new SurahInfo(47, "محمد", 38, 26, 0.195m),
            new SurahInfo(48, "الفتح", 29, 26, 0.149m),
            new SurahInfo(49, "الحجرات", 18, 26, 0.092m),
            new SurahInfo(50, "ق", 45, 26, 0.231m),
            new SurahInfo(51, "الذاريات", 60, 26, 0.229m),
            new SurahInfo(52, "الطور", 49, 27, 0.123m),
            new SurahInfo(53, "النجم", 62, 27, 0.155m),
            new SurahInfo(54, "القمر", 55, 27, 0.138m),
            new SurahInfo(55, "الرحمن", 78, 27, 0.195m),
            new SurahInfo(56, "الواقعة", 96, 27, 0.241m),
            new SurahInfo(57, "الحديد", 29, 27, 0.073m),
            new SurahInfo(58, "المجادلة", 22, 28, 0.161m),
            new SurahInfo(59, "الحشر", 24, 28, 0.175m),
            new SurahInfo(60, "الممتحنة", 13, 28, 0.095m),
            new SurahInfo(61, "الصف", 14, 28, 0.102m),
            new SurahInfo(62, "الجمعة", 11, 28, 0.080m),
            new SurahInfo(63, "المنافقون", 11, 28, 0.080m),
            new SurahInfo(64, "التغابن", 18, 28, 0.131m),
            new SurahInfo(65, "الطلاق", 12, 28, 0.088m),
            new SurahInfo(66, "التحريم", 12, 28, 0.088m),
            new SurahInfo(67, "الملك", 30, 29, 0.070m),
            new SurahInfo(68, "القلم", 52, 29, 0.121m),
            new SurahInfo(69, "الحاقة", 52, 29, 0.121m),
            new SurahInfo(70, "المعارج", 44, 29, 0.102m),
            new SurahInfo(71, "نوح", 28, 29, 0.065m),
            new SurahInfo(72, "الجن", 28, 29, 0.065m),
            new SurahInfo(73, "المزمل", 20, 29, 0.046m),
            new SurahInfo(74, "المدثر", 56, 29, 0.130m),
            new SurahInfo(75, "القيامة", 40, 29, 0.093m),
            new SurahInfo(76, "الإنسان", 31, 29, 0.072m),
            new SurahInfo(77, "المرسلات", 50, 29, 0.116m),
            new SurahInfo(78, "النبأ", 40, 30, 0.071m),
            new SurahInfo(79, "النازعات", 46, 30, 0.082m),
            new SurahInfo(80, "عبس", 42, 30, 0.074m),
            new SurahInfo(81, "التكوير", 29, 30, 0.051m),
            new SurahInfo(82, "الانفطار", 19, 30, 0.034m),
            new SurahInfo(83, "المطففين", 36, 30, 0.064m),
            new SurahInfo(84, "الانشقاق", 25, 30, 0.044m),
            new SurahInfo(85, "البروج", 22, 30, 0.039m),
            new SurahInfo(86, "الطارق", 17, 30, 0.030m),
            new SurahInfo(87, "الأعلى", 19, 30, 0.034m),
            new SurahInfo(88, "الغاشية", 26, 30, 0.046m),
            new SurahInfo(89, "الفجر", 30, 30, 0.053m),
            new SurahInfo(90, "البلد", 20, 30, 0.035m),
            new SurahInfo(91, "الشمس", 15, 30, 0.027m),
            new SurahInfo(92, "الليل", 21, 30, 0.037m),
            new SurahInfo(93, "الضحى", 11, 30, 0.020m),
            new SurahInfo(94, "الشرح", 8, 30, 0.014m),
            new SurahInfo(95, "التين", 8, 30, 0.014m),
            new SurahInfo(96, "العلق", 19, 30, 0.034m),
            new SurahInfo(97, "القدر", 5, 30, 0.009m),
            new SurahInfo(98, "البينة", 8, 30, 0.014m),
            new SurahInfo(99, "الزلزلة", 8, 30, 0.014m),
            new SurahInfo(100, "العاديات", 11, 30, 0.020m),
            new SurahInfo(101, "القارعة", 11, 30, 0.020m),
            new SurahInfo(102, "التكاثر", 8, 30, 0.014m),
            new SurahInfo(103, "العصر", 3, 30, 0.005m),
            new SurahInfo(104, "الهمزة", 9, 30, 0.016m),
            new SurahInfo(105, "الفيل", 5, 30, 0.009m),
            new SurahInfo(106, "قريش", 4, 30, 0.007m),
            new SurahInfo(107, "الماعون", 7, 30, 0.012m),
            new SurahInfo(108, "الكوثر", 3, 30, 0.005m),
            new SurahInfo(109, "الكافرون", 6, 30, 0.011m),
            new SurahInfo(110, "النصر", 3, 30, 0.005m),
            new SurahInfo(111, "المسد", 5, 30, 0.009m),
            new SurahInfo(112, "الإخلاص", 4, 30, 0.007m),
            new SurahInfo(113, "الفلق", 5, 30, 0.009m),
            new SurahInfo(114, "الناس", 6, 30, 0.011m),
        };

        public SurahInfo? GetSurahByNumber(int number)
        {
            return _surahs.FirstOrDefault(s => s.Number == number);
        }

        public SurahInfo? GetSurahByName(string name)
        {
            return _surahs.FirstOrDefault(s => s.Name == name);
        }

        public IEnumerable<SurahInfo> GetAllSurahs()
        {
            return _surahs;
        }

        public decimal CalculateJuzMemorized(MemorizationDirection direction, int currentSurahNumber, int currentVerse)
        {
            if (currentVerse == 0)
            {
                if (direction == MemorizationDirection.Forward)
                {
                    return _surahs.Where(s => s.Number < currentSurahNumber).Sum(s => s.JuzPortion);
                }
                else
                {
                    return _surahs.Where(s => s.Number > currentSurahNumber).Sum(s => s.JuzPortion);
                }
            }

            var currentSurah = GetSurahByNumber(currentSurahNumber);
            if (currentSurah == null) return 0;

            decimal partialProgress = (decimal)currentVerse / currentSurah.VerseCount * currentSurah.JuzPortion;

            if (direction == MemorizationDirection.Forward)
            {
                return _surahs.Where(s => s.Number < currentSurahNumber).Sum(s => s.JuzPortion) + partialProgress;
            }
            else
            {
                return _surahs.Where(s => s.Number > currentSurahNumber).Sum(s => s.JuzPortion) + partialProgress;
            }
        }

        public (int nextSurah, int nextVerse) GetNextPosition(MemorizationDirection direction, int surahNumber, int toVerse)
        {
            var surah = GetSurahByNumber(surahNumber);
            if (surah == null) return (surahNumber, toVerse);

            if (toVerse >= surah.VerseCount)
            {
                if (direction == MemorizationDirection.Forward)
                {
                    return surahNumber < 114 ? (surahNumber + 1, 0) : (114, surah.VerseCount);
                }
                else
                {
                    return surahNumber > 1 ? (surahNumber - 1, 0) : (1, surah.VerseCount);
                }
            }

            return (surahNumber, toVerse);
        }
    }
}

