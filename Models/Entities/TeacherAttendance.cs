using System;
using System.ComponentModel.DataAnnotations;

namespace KhairAPI.Models.Entities
{
    public class TeacherAttendance : ITenantEntity
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

        /// <summary>Arrival time (KSA local time of day). Null until checked in with a time.</summary>
        public TimeOnly? CheckInTime { get; set; }

        /// <summary>Departure time (KSA local time of day). Null until the teacher checks out.</summary>
        public TimeOnly? CheckOutTime { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Multi-tenancy
        public int AssociationId { get; set; }
        public Association? Association { get; set; }

        // Navigation properties
        public Teacher Teacher { get; set; } = null!;
        public Halaqa Halaqa { get; set; } = null!;
    }
}








