using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KhairAPI.Models.Entities
{
    public class Halaqa : ITenantEntity
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Location { get; set; }

        public string? TimeSlot { get; set; }

        /// <summary>
        /// Comma-separated days of week (0=Sunday, 1=Monday, etc.)
        /// Example: "0,1,3,4" = Sunday, Monday, Wednesday, Thursday
        /// </summary>
        public string? ActiveDays { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Multi-tenancy
        public int AssociationId { get; set; }
        public Association? Association { get; set; }

        // Navigation properties
        public ICollection<HalaqaTeacher> HalaqaTeachers { get; set; } = new List<HalaqaTeacher>();
        public ICollection<StudentHalaqa> StudentHalaqat { get; set; } = new List<StudentHalaqa>();
        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
        public ICollection<ProgressRecord> ProgressRecords { get; set; } = new List<ProgressRecord>();
        public ICollection<TeacherAttendance> TeacherAttendances { get; set; } = new List<TeacherAttendance>();
        
        /// <summary>
        /// HalaqaSupervisors assigned to manage this halaqa
        /// </summary>
        public ICollection<HalaqaSupervisorAssignment> SupervisorAssignments { get; set; } = new List<HalaqaSupervisorAssignment>();
    }
}

