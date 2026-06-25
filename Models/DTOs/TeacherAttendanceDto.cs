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
        public TimeOnly? CheckInTime { get; set; }   // وقت الحضور
        public TimeOnly? CheckOutTime { get; set; }  // وقت الانصراف
        public double? WorkedHours { get; set; }     // ساعات العمل (انصراف - حضور)
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

        public TimeOnly? CheckInTime { get; set; }   // وقت الحضور (اختياري)
        public TimeOnly? CheckOutTime { get; set; }  // وقت الانصراف (اختياري)

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
        public TimeOnly? CheckInTime { get; set; }   // وقت الحضور
        public TimeOnly? CheckOutTime { get; set; }  // وقت الانصراف
        public double? WorkedHours { get; set; }     // ساعات العمل
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

        public TimeOnly? CheckInTime { get; set; }   // وقت الحضور
        public TimeOnly? CheckOutTime { get; set; }  // وقت الانصراف

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
        public double TotalHours { get; set; }     // إجمالي ساعات العمل خلال الشهر
        public double AttendanceRate => ExpectedDays > 0 ? (double)(PresentDays + LateDays) / ExpectedDays * 100 : 0;
    }

    /// <summary>
    /// A teacher's own attendance status for a single halaqa today (self check-in feature).
    /// Each halaqa active today is checked in / out independently.
    /// </summary>
    public class TeacherSelfHalaqaAttendanceDto
    {
        public int HalaqaId { get; set; }
        public string HalaqaName { get; set; } = string.Empty;
        public string? TimeSlot { get; set; }
        /// <summary>Recorded status for this halaqa today, or null if nothing recorded yet.</summary>
        public AttendanceStatus? Status { get; set; }
        /// <summary>True if a present record exists for this halaqa today.</summary>
        public bool CheckedIn { get; set; }
        /// <summary>True if a departure time is recorded for this halaqa today.</summary>
        public bool CheckedOut { get; set; }
        public TimeOnly? CheckInTime { get; set; }
        public TimeOnly? CheckOutTime { get; set; }
    }

    /// <summary>
    /// A teacher's own attendance status for today (self check-in feature).
    /// Attendance is recorded per halaqa, so the teacher sees one entry per halaqa active today.
    /// </summary>
    public class TeacherSelfAttendanceStatusDto
    {
        public DateTime Date { get; set; }
        public string DayName { get; set; } = string.Empty; // Arabic day name
        /// <summary>True when every halaqa active today has a present record.</summary>
        public bool CheckedIn { get; set; }
        /// <summary>True when every halaqa active today has a departure time recorded.</summary>
        public bool CheckedOut { get; set; }
        /// <summary>False when the teacher has no halaqa scheduled today (nothing to check in to).</summary>
        public bool HasActiveHalaqaToday { get; set; }
        /// <summary>Number of the teacher's halaqat that are active today.</summary>
        public int HalaqatCount { get; set; }
        /// <summary>The teacher's halaqat active today, each checked in / out independently.</summary>
        public List<TeacherSelfHalaqaAttendanceDto> Halaqat { get; set; } = new List<TeacherSelfHalaqaAttendanceDto>();
    }

    /// <summary>
    /// Result of a teacher self check-in
    /// </summary>
    public class TeacherSelfCheckInResultDto
    {
        public bool CheckedIn { get; set; }
        public int RecordsCreated { get; set; }
        public string Message { get; set; } = string.Empty;
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
        public double TotalHours { get; set; }  // إجمالي ساعات العمل لكل المعلمين
        public List<TeacherMonthlySummaryDto> Teachers { get; set; } = new List<TeacherMonthlySummaryDto>();
    }
}








