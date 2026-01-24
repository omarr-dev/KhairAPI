using System;
using System.ComponentModel.DataAnnotations;
using KhairAPI.Core.Attributes;

namespace KhairAPI.Models.DTOs
{
    public class TeacherDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? IdNumber { get; set; }
        public string? Qualification { get; set; }
        public DateTime JoinDate { get; set; }
        public int HalaqatCount { get; set; }
        public int StudentsCount { get; set; }
    }

    public class CreateTeacherDto
    {
        [Required(ErrorMessage = "رقم الجوال مطلوب")]
        [SaudiPhoneNumber(ErrorMessage = "رقم الجوال يجب أن يكون سعودي ويبدأ بـ +966 5")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "الاسم الكامل مطلوب")]
        public string FullName { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صحيح")]
        public string? Email { get; set; }

        public string? IdNumber { get; set; }

        public string? Qualification { get; set; }

        public int? HalaqaId { get; set; }
    }

    public class UpdateTeacherDto
    {
        [Required(ErrorMessage = "الاسم الكامل مطلوب")]
        public string FullName { get; set; } = string.Empty;

        [SaudiPhoneNumber(ErrorMessage = "رقم الجوال يجب أن يكون سعودي ويبدأ بـ +966 5")]
        public string? PhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صحيح")]
        public string? Email { get; set; }

        public string? IdNumber { get; set; }

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
