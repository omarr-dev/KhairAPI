using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using KhairAPI.Models.Entities;

namespace KhairAPI.Models.DTOs
{
    /// <summary>
    /// Teacher attendance record response DTO
    /// </summary>
    public class TeacherAttendanceRecordDto
    {
        public int Id { get; set; }
        public int TeacherId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public int HalaqaId { get; set; }
        public string HalaqaName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Status { get; set; } = string.Empty; // حاضر/غائب/متأخر
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Single teacher attendance entry for bulk operations
    /// </summary>
    public class TeacherAttendanceEntryDto
    {
        [Required(ErrorMessage = "المعلم مطلوب")]
        public int TeacherId { get; set; }

        [Required(ErrorMessage = "الحلقة مطلوبة")]
        public int HalaqaId { get; set; }

        [Required(ErrorMessage = "حالة الحضور مطلوبة")]
        public AttendanceStatus Status { get; set; }

        public string? Notes { get; set; }
    }

    /// <summary>
    /// Bulk teacher attendance for today
    /// </summary>
    public class BulkTeacherAttendanceDto
    {
        [Required(ErrorMessage = "سجلات الحضور مطلوبة")]
        public List<TeacherAttendanceEntryDto> Attendance { get; set; } = new List<TeacherAttendanceEntryDto>();
    }

    /// <summary>
    /// Teacher info with attendance status for a specific halaqa
    /// </summary>
    public class TeacherWithAttendanceDto
    {
        public int TeacherId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public int? AttendanceId { get; set; } // null if no attendance recorded yet
        public AttendanceStatus? Status { get; set; } // null if no attendance recorded
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Halaqa with its teachers and their attendance status for today
    /// </summary>
    public class HalaqaTeachersAttendanceDto
    {
        public int HalaqaId { get; set; }
        public string HalaqaName { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string? TimeSlot { get; set; }
        public bool IsActiveToday { get; set; }
        public List<TeacherWithAttendanceDto> Teachers { get; set; } = new List<TeacherWithAttendanceDto>();
    }

    /// <summary>
    /// Response for today's teacher attendance page
    /// </summary>
    public class TodayTeacherAttendanceResponseDto
    {
        public DateTime Date { get; set; }
        public string DayName { get; set; } = string.Empty; // Arabic day name
        public int TotalTeachers { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public int LateCount { get; set; }
        public double AttendanceRate => TotalTeachers > 0 ? (double)PresentCount / TotalTeachers * 100 : 0;
        public List<HalaqaTeachersAttendanceDto> Halaqat { get; set; } = new List<HalaqaTeachersAttendanceDto>();
    }

    /// <summary>
    /// Update teacher attendance DTO
    /// </summary>
    public class UpdateTeacherAttendanceDto
    {
        [Required(ErrorMessage = "حالة الحضور مطلوبة")]
        public AttendanceStatus Status { get; set; }
        
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Monthly summary for a single teacher (for salary calculation)
    /// </summary>
    public class TeacherMonthlySummaryDto
    {
        public int TeacherId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public int ExpectedDays { get; set; }      // Based on Halaqa active days
        public int PresentDays { get; set; }       // Includes Late (no deduction)
        public int AbsentDays { get; set; }        // For salary deduction
        public int LateDays { get; set; }          // Info only, no deduction
        public double AttendanceRate => ExpectedDays > 0 ? (double)(PresentDays + LateDays) / ExpectedDays * 100 : 0;
    }

    /// <summary>
    /// Monthly attendance report for all teachers (for salary calculation)
    /// </summary>
    public class MonthlyAttendanceReportDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;  // Arabic month name
        public int TotalTeachers { get; set; }
        public int TotalExpectedDays { get; set; }
        public int TotalPresentDays { get; set; }
        public int TotalAbsentDays { get; set; }
        public List<TeacherMonthlySummaryDto> Teachers { get; set; } = new List<TeacherMonthlySummaryDto>();
    }
}







