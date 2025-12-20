using System.ComponentModel.DataAnnotations;
using KhairAPI.Core.Attributes;
using KhairAPI.Models.Entities;

namespace KhairAPI.Models.DTOs
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "رقم الجوال مطلوب")]
        [SaudiPhoneNumber(ErrorMessage = "رقم الجوال يجب أن يكون سعودي ويبدأ بـ +966 5")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "الاسم الكامل مطلوب")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "الصلاحية مطلوبة")]
        public UserRole Role { get; set; }

        // For teachers only
        public string? Qualification { get; set; }
    }
}
