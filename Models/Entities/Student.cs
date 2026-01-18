using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KhairAPI.Models.Entities
{
    public class Student : ITenantEntity
    {
        public int Id { get; set; }

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        public DateTime? DateOfBirth { get; set; }

        public string? GuardianName { get; set; }

        public string? GuardianPhone { get; set; }

        // Student contact info
        public string? Phone { get; set; }
        
        public string? IdNumber { get; set; }

        // Memorization tracking
        public MemorizationDirection MemorizationDirection { get; set; } = MemorizationDirection.Forward;

        public int CurrentSurahNumber { get; set; } = 1; // 1-114, starts at Al-Fatihah

        public int CurrentVerse { get; set; } = 0; // 0 means hasn't started this surah yet

        public decimal JuzMemorized { get; set; } = 0; // Auto-calculated based on position

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Multi-tenancy
        public int AssociationId { get; set; }
        public Association? Association { get; set; }

        // Navigation properties
        public ICollection<StudentHalaqa> StudentHalaqat { get; set; } = new List<StudentHalaqa>();
        public ICollection<ProgressRecord> ProgressRecords { get; set; } = new List<ProgressRecord>();
        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

        // Target and achievement tracking
        public StudentTarget? Target { get; set; }
        public ICollection<TargetAchievement> TargetAchievements { get; set; } = new List<TargetAchievement>();

        // Computed property for full name
        public string FullName => $"{FirstName} {LastName}";
    }

    public enum MemorizationDirection
    {
        Forward,  // من الفاتحة إلى الناس
        Backward  // من الناس إلى الفاتحة
    }
}
