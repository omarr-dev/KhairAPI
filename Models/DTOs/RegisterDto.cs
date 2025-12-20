using System.ComponentModel.DataAnnotations;
using KhairAPI.Models.Entities;

namespace KhairAPI.Models.DTOs
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صالح")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [MinLength(6, ErrorMessage = "كلمة المرور يجب أن تكون 6 أحرف على الأقل")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "الاسم الكامل مطلوب")]
        public string FullName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "رقم الهاتف غير صالح")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "الصلاحية مطلوبة")]
        public UserRole Role { get; set; }

        // For teachers only
        public string? Qualification { get; set; }
    }
}
