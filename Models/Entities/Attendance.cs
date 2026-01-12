using System;
using System.ComponentModel.DataAnnotations;

namespace KhairAPI.Models.Entities
{
    public class Attendance
    {
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        public int HalaqaId { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public AttendanceStatus Status { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Multi-tenancy
        public int AssociationId { get; set; }
        public Association? Association { get; set; }

        // Navigation properties
        public Student Student { get; set; } = null!;
        public Halaqa Halaqa { get; set; } = null!;
    }

    public enum AttendanceStatus
    {
        Present,  // حاضر
        Absent,   // غائب
        Late      // متأخر
    }
}
