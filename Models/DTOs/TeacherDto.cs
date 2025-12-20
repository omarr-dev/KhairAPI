using System;
using System.ComponentModel.DataAnnotations;

namespace KhairAPI.Models.DTOs
{
    public class TeacherDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Qualification { get; set; }
        public DateTime JoinDate { get; set; }
        public int HalaqatCount { get; set; }
        public int StudentsCount { get; set; }
    }

    public class CreateTeacherDto
    {
        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صالح")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [MinLength(6, ErrorMessage = "كلمة المرور يجب أن تكون 6 أحرف على الأقل")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "الاسم الكامل مطلوب")]
        public string FullName { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }
        public string? Qualification { get; set; }
    }

    public class UpdateTeacherDto
    {
        [Required(ErrorMessage = "الاسم الكامل مطلوب")]
        public string FullName { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }
        public string? Qualification { get; set; }
    }

    public class TeacherHalaqaDto
    {
        public int HalaqaId { get; set; }
        public string HalaqaName { get; set; } = string.Empty;
        public DateTime AssignedDate { get; set; }
        public bool IsPrimary { get; set; }
    }
}
