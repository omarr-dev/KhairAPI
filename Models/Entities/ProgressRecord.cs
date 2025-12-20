using System;
using System.ComponentModel.DataAnnotations;

namespace KhairAPI.Models.Entities
{
    public class ProgressRecord
    {
        public int Id { get; set; }
        
        [Required]
        public int StudentId { get; set; }
        
        public int? TeacherId { get; set; }
        
        [Required]
        public int HalaqaId { get; set; }
        
        [Required]
        public DateTime Date { get; set; }
        
        [Required]
        public ProgressType Type { get; set; }
        
        [Required]
        public string SurahName { get; set; } = string.Empty;
        
        [Required]
        public int FromVerse { get; set; }
        
        [Required]
        public int ToVerse { get; set; }
        
        public QualityRating Quality { get; set; }
        
        public string? Notes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public Student Student { get; set; } = null!;
        public Teacher? Teacher { get; set; }
        public Halaqa Halaqa { get; set; } = null!;
    }
    
    public enum ProgressType
    {
        Memorization, // حفظ
        Revision      // مراجعة
    }
    
    public enum QualityRating
    {
        Excellent,   // ممتاز
        VeryGood,    // جيد جداً
        Good,        // جيد
        Acceptable   // مقبول
    }
}
