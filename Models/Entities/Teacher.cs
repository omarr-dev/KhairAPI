using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KhairAPI.Models.Entities
{
    public class Teacher
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public string FullName { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        public string? Qualification { get; set; }

        public DateTime JoinDate { get; set; } = DateTime.UtcNow;

        // Multi-tenancy
        public int AssociationId { get; set; }
        public Association? Association { get; set; }

        // Navigation properties
        public User User { get; set; } = null!;
        public ICollection<HalaqaTeacher> HalaqaTeachers { get; set; } = new List<HalaqaTeacher>();
        public ICollection<StudentHalaqa> StudentHalaqat { get; set; } = new List<StudentHalaqa>();
        public ICollection<ProgressRecord> ProgressRecords { get; set; } = new List<ProgressRecord>();
        public ICollection<TeacherAttendance> TeacherAttendances { get; set; } = new List<TeacherAttendance>();
    }
}
