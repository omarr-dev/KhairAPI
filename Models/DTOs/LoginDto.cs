using System.ComponentModel.DataAnnotations;
using KhairAPI.Core.Attributes;

namespace KhairAPI.Models.DTOs
{
    public class LoginDto
    {
        [Required(ErrorMessage = "رقم الجوال مطلوب")]
        [SaudiPhoneNumber(ErrorMessage = "رقم الجوال يجب أن يكون سعودي ويبدأ بـ +966 5")]
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
