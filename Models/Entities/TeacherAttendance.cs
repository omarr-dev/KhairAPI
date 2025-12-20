using System;
using System.ComponentModel.DataAnnotations;

namespace KhairAPI.Models.Entities
{
    public class TeacherAttendance
    {
        public int Id { get; set; }
        
        [Required]
        public int TeacherId { get; set; }
        
        [Required]
        public int HalaqaId { get; set; }
        
        [Required]
        public DateTime Date { get; set; }
        
        [Required]
        public AttendanceStatus Status { get; set; }
        
        public string? Notes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public Teacher Teacher { get; set; } = null!;
        public Halaqa Halaqa { get; set; } = null!;
    }
}








