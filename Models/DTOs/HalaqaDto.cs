using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KhairAPI.Models.DTOs
{
    public class HalaqaDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Location { get; set; }
        public string? TimeSlot { get; set; }
        /// <summary>
        /// Comma-separated days of week (0=Sunday, 1=Monday, etc.)
        /// </summary>
        public string? ActiveDays { get; set; }
        public bool IsActive { get; set; }
        public int StudentCount { get; set; }
        public int TeacherCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateHalaqaDto
    {
        [Required(ErrorMessage = "اسم الحلقة مطلوب")]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
        public string? Location { get; set; }
        public string? TimeSlot { get; set; }
        /// <summary>
        /// Comma-separated days of week (0=Sunday, 1=Monday, etc.)
        /// </summary>
        public string? ActiveDays { get; set; }
    }

    public class UpdateHalaqaDto
    {
        [Required(ErrorMessage = "اسم الحلقة مطلوب")]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
        public string? Location { get; set; }
        public string? TimeSlot { get; set; }
        /// <summary>
        /// Comma-separated days of week (0=Sunday, 1=Monday, etc.)
        /// </summary>
        public string? ActiveDays { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Hierarchical view of Halaqa with nested teachers and students
    /// </summary>
    public class HalaqaHierarchyDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Location { get; set; }
        public string? TimeSlot { get; set; }
        public string? ActiveDays { get; set; }
        public bool IsActive { get; set; }
        public int StudentCount { get; set; }
        public int TeacherCount { get; set; }
        public List<TeacherInHalaqaDto> Teachers { get; set; } = new List<TeacherInHalaqaDto>();
    }

    /// <summary>
    /// Teacher with their students in a specific Halaqa
    /// </summary>
    public class TeacherInHalaqaDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public int StudentCount { get; set; }
        public List<StudentInHalaqaDto> Students { get; set; } = new List<StudentInHalaqaDto>();
    }

    /// <summary>
    /// Simplified student info for hierarchy view
    /// </summary>
    public class StudentInHalaqaDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string MemorizationDirection { get; set; } = "Forward";
        public int CurrentSurahNumber { get; set; }
        public string? CurrentSurahName { get; set; }
        public int CurrentVerse { get; set; }
        public decimal JuzMemorized { get; set; }
    }
}
