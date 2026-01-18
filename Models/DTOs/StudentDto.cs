using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using KhairAPI.Models.Entities;

namespace KhairAPI.Models.DTOs
{
    public class StudentDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}";
        public DateTime? DateOfBirth { get; set; }
        public string? GuardianName { get; set; }
        public string? GuardianPhone { get; set; }
        public string? Phone { get; set; }
        public string? IdNumber { get; set; }
        
        // Memorization tracking
        public string MemorizationDirection { get; set; } = "Forward";
        public int CurrentSurahNumber { get; set; }
        public string? CurrentSurahName { get; set; }
        public int CurrentVerse { get; set; }
        public decimal JuzMemorized { get; set; }
        
        public string? CurrentHalaqa { get; set; }
        public string? TeacherName { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<StudentAssignmentDto> Assignments { get; set; } = new List<StudentAssignmentDto>();
    }

    public class StudentAssignmentDto
    {
        public int StudentId { get; set; }
        public int HalaqaId { get; set; }
        public string HalaqaName { get; set; } = string.Empty;
        public int TeacherId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public DateTime EnrollmentDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateStudentDto
    {
        [Required(ErrorMessage = "الاسم الأول مطلوب")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "اسم العائلة مطلوب")]
        public string LastName { get; set; } = string.Empty;

        public DateTime? DateOfBirth { get; set; }

        public string? GuardianName { get; set; }

        [RegularExpression(@"^\+9665\d{8}$", ErrorMessage = "رقم هاتف ولي الأمر غير صالح.")]
        public string? GuardianPhone { get; set; }

        [RegularExpression(@"^\+9665\d{8}$", ErrorMessage = "رقم هاتف الطالب ")]
        public string? Phone { get; set; }

        public string? IdNumber { get; set; }

        // Memorization tracking
        public string MemorizationDirection { get; set; } = "Forward";
        public int CurrentSurahNumber { get; set; } = 1;
        public int CurrentVerse { get; set; } = 0;

        public int? HalaqaId { get; set; }
        
        public int? TeacherId { get; set; }
    }

    public class UpdateStudentDto
    {
        [Required(ErrorMessage = "الاسم الأول مطلوب")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "اسم العائلة مطلوب")]
        public string LastName { get; set; } = string.Empty;

        public DateTime? DateOfBirth { get; set; }

        public string? GuardianName { get; set; }

        [RegularExpression(@"^\+9665\d{8}$", ErrorMessage = "رقم هاتف ولي الأمر غير صالح. يجب أن يبدأ بـ +966")]
        public string? GuardianPhone { get; set; }

        [RegularExpression(@"^\+9665\d{8}$", ErrorMessage = "رقم هاتف الطالب غير صالح. يجب أن يبدأ بـ +966")]
        public string? Phone { get; set; }

        public string? IdNumber { get; set; }
    }
    
    public class UpdateMemorizationDto
    {
        [Required(ErrorMessage = "اتجاه الحفظ مطلوب")]
        public string MemorizationDirection { get; set; } = "Forward";
        
        [Required(ErrorMessage = "رقم السورة مطلوب")]
        [Range(1, 114, ErrorMessage = "رقم السورة يجب أن يكون بين 1 و 114")]
        public int CurrentSurahNumber { get; set; }
        
        [Required(ErrorMessage = "رقم الآية مطلوب")]
        [Range(0, 286, ErrorMessage = "رقم الآية غير صالح")]
        public int CurrentVerse { get; set; }
    }

    public class AssignStudentDto
    {
        [Required(ErrorMessage = "معرف الطالب مطلوب")]
        public int StudentId { get; set; }

        [Required(ErrorMessage = "معرف الحلقة مطلوب")]
        public int HalaqaId { get; set; }

        [Required(ErrorMessage = "معرف المعلم مطلوب")]
        public int TeacherId { get; set; }
    }

    public class UpdateAssignmentDto
    {
        [Required(ErrorMessage = "معرف الحلقة مطلوب")]
        public int HalaqaId { get; set; }

        [Required(ErrorMessage = "معرف المعلم مطلوب")]
        public int TeacherId { get; set; }

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Comprehensive student detail for profile page
    /// </summary>
    public class StudentDetailDto
    {
        // Basic Info
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}";
        public DateTime? DateOfBirth { get; set; }
        public string? GuardianName { get; set; }
        public string? GuardianPhone { get; set; }
        public string? Phone { get; set; }
        public string? IdNumber { get; set; }
        
        // Memorization tracking
        public string MemorizationDirection { get; set; } = "Forward";
        public int CurrentSurahNumber { get; set; }
        public string? CurrentSurahName { get; set; }
        public int CurrentVerse { get; set; }
        public decimal JuzMemorized { get; set; }
        
        // Halaqa Info
        public int? HalaqaId { get; set; }
        public string? CurrentHalaqa { get; set; }
        public string? HalaqaActiveDays { get; set; } // "0,1,3,4" = Sun,Mon,Wed,Thu
        public string? TeacherName { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // Statistics
        public StudentStatsDto Stats { get; set; } = new StudentStatsDto();
        
        // Recent Records
        public List<ProgressRecordDto> RecentProgress { get; set; } = new List<ProgressRecordDto>();
        public List<AttendanceRecordDto> RecentAttendance { get; set; } = new List<AttendanceRecordDto>();
    }

    /// <summary>
    /// Student statistics for profile page
    /// </summary>
    public class StudentStatsDto
    {
        public int TotalVersesMemorized { get; set; }
        public int TotalVersesRevised { get; set; }
        public double AttendanceRate { get; set; } // Percentage
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int LateDays { get; set; }
        public int TotalClassDays { get; set; }
        public double AverageQuality { get; set; } // 0-3 scale (0=Excellent, 3=Acceptable)
        public string AverageQualityText { get; set; } = string.Empty; // ممتاز، جيد جداً، etc.
        public int TotalProgressRecords { get; set; }
    }
}
