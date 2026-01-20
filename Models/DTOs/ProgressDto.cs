using System;
using System.ComponentModel.DataAnnotations;
using KhairAPI.Models.Entities;

namespace KhairAPI.Models.DTOs
{
    public class ProgressRecordDto
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public int? TeacherId { get; set; }
        public string? TeacherName { get; set; }
        public int HalaqaId { get; set; }
        public string HalaqaName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Type { get; set; } = string.Empty; // حفظ/مراجعة
        public string SurahName { get; set; } = string.Empty;
        public int FromVerse { get; set; }
        public int ToVerse { get; set; }
        public string Quality { get; set; } = string.Empty; // ممتاز/جيد جداً/جيد/مقبول
        public string? Notes { get; set; }
        
        /// <summary>
        /// عدد الأسطر في المصحف لهذا التسميع
        /// </summary>
        public double NumberLines { get; set; }
        
        public DateTime CreatedAt { get; set; }
    }

    public class CreateProgressRecordDto
    {
        [Required(ErrorMessage = "الطالب مطلوب")]
        public int StudentId { get; set; }

        [Required(ErrorMessage = "المعلم مطلوب")]
        public int TeacherId { get; set; }

        [Required(ErrorMessage = "الحلقة مطلوبة")]
        public int HalaqaId { get; set; }

        [Required(ErrorMessage = "التاريخ مطلوب")]
        public DateTime Date { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "نوع التسميع مطلوب")]
        public ProgressType Type { get; set; }

        [Required(ErrorMessage = "اسم السورة مطلوب")]
        public string SurahName { get; set; } = string.Empty;

        [Required(ErrorMessage = "آية البداية مطلوبة")]
        [Range(1, int.MaxValue, ErrorMessage = "آية البداية يجب أن تكون أكبر من صفر")]
        public int FromVerse { get; set; }

        [Required(ErrorMessage = "آية النهاية مطلوبة")]
        [Range(1, int.MaxValue, ErrorMessage = "آية النهاية يجب أن تكون أكبر من صفر")]
        public int ToVerse { get; set; }

        [Required(ErrorMessage = "التقييم مطلوب")]
        public QualityRating Quality { get; set; }

        public string? Notes { get; set; }
    }

    public class DailyProgressSummaryDto
    {
        public DateTime Date { get; set; }
        public int TotalMemorization { get; set; }
        public int TotalRevision { get; set; }
        public int UniqueStudents { get; set; }
        public List<ProgressRecordDto> Records { get; set; } = new List<ProgressRecordDto>();
    }

    public class StudentProgressSummaryDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public int TotalMemorized { get; set; }
        public int TotalRevised { get; set; }
        public DateTime LastProgressDate { get; set; }
        public double AverageQuality { get; set; }
        public List<ProgressRecordDto> RecentProgress { get; set; } = new List<ProgressRecordDto>();
    }
}
