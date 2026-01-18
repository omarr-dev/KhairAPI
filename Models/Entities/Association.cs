using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KhairAPI.Models.Entities
{
    /// <summary>
    /// Represents a Quranic association (جمعية) in the multi-tenant system.
    /// Each association has its own isolated data (users, students, halaqat).
    /// </summary>
    public class Association
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Subdomain { get; set; } = string.Empty; // For future use when domain is purchased

        [MaxLength(500)]
        public string? Logo { get; set; } // URL to logo image

        [MaxLength(10)]
        public string? PrimaryColor { get; set; } // Hex color code (e.g., #1e3a5f)

        [MaxLength(10)]
        public string? SecondaryColor { get; set; } // Hex color code

        // Additional branding and contact information
        [MaxLength(255)]
        public string? DisplayName { get; set; } // Display name (can be different from Name)

        [MaxLength(1000)]
        public string? Description { get; set; } // About the association

        [MaxLength(500)]
        public string? Favicon { get; set; } // URL to favicon image

        [MaxLength(20)]
        public string? PhoneNumber { get; set; } // Contact phone number

        [MaxLength(255)]
        public string? Email { get; set; } // Contact email

        [MaxLength(255)]
        public string? ManagerName { get; set; } // Name of the association manager

        [MaxLength(100)]
        public string? Country { get; set; } // Country

        [MaxLength(100)]
        public string? City { get; set; } // City

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<Teacher> Teachers { get; set; } = new List<Teacher>();
        public ICollection<Student> Students { get; set; } = new List<Student>();
        public ICollection<Halaqa> Halaqat { get; set; } = new List<Halaqa>();
        public ICollection<HalaqaTeacher> HalaqaTeachers { get; set; } = new List<HalaqaTeacher>();
        public ICollection<StudentHalaqa> StudentHalaqat { get; set; } = new List<StudentHalaqa>();
        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
        public ICollection<ProgressRecord> ProgressRecords { get; set; } = new List<ProgressRecord>();
        public ICollection<TeacherAttendance> TeacherAttendances { get; set; } = new List<TeacherAttendance>();
        public ICollection<StudentTarget> StudentTargets { get; set; } = new List<StudentTarget>();
        public ICollection<TargetAchievement> TargetAchievements { get; set; } = new List<TargetAchievement>();
    }
}
