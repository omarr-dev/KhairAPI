using System;
using System.ComponentModel.DataAnnotations;
using KhairAPI.Models.Entities;

namespace KhairAPI.Models.DTOs
{
    public class AttendanceRecordDto
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public int HalaqaId { get; set; }
        public string HalaqaName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Status { get; set; } = string.Empty; // حاضر/غائب/متأخر
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateAttendanceDto
    {
        [Required(ErrorMessage = "الطالب مطلوب")]
        public int StudentId { get; set; }

        [Required(ErrorMessage = "الحلقة مطلوبة")]
        public int HalaqaId { get; set; }

        [Required(ErrorMessage = "التاريخ مطلوب")]
        public DateTime Date { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "حالة الحضور مطلوبة")]
        public AttendanceStatus Status { get; set; }

        public string? Notes { get; set; }
    }

    public class BulkAttendanceDto
    {
        [Required(ErrorMessage = "الحلقة مطلوبة")]
        public int HalaqaId { get; set; }

        [Required(ErrorMessage = "التاريخ مطلوب")]
        public DateTime Date { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "سجلات الحضور مطلوبة")]
        public List<StudentAttendanceDto> Attendance { get; set; } = new List<StudentAttendanceDto>();
    }

    public class StudentAttendanceDto
    {
        [Required(ErrorMessage = "الطالب مطلوب")]
        public int StudentId { get; set; }

        [Required(ErrorMessage = "حالة الحضور مطلوبة")]
        public AttendanceStatus Status { get; set; }

        public string? Notes { get; set; }
    }

    public class AttendanceSummaryDto
    {
        public DateTime Date { get; set; }
        public int HalaqaId { get; set; }
        public string HalaqaName { get; set; } = string.Empty;
        public int TotalStudents { get; set; }
        public int Present { get; set; }
        public int Absent { get; set; }
        public int Late { get; set; }
        public double AttendanceRate => TotalStudents > 0 ? (double)Present / TotalStudents * 100 : 0;
        public List<AttendanceRecordDto> Records { get; set; } = new List<AttendanceRecordDto>();
    }

    public class StudentAttendanceSummaryDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public int TotalDays { get; set; }
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int LateDays { get; set; }
        public double AttendanceRate => TotalDays > 0 ? (double)PresentDays / TotalDays * 100 : 0;
    }
}
