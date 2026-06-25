using System.ComponentModel.DataAnnotations;
using KhairAPI.Core.Attributes;

namespace KhairAPI.Models.DTOs
{
    /// <summary>
    /// Payload for updating an existing user's basic details (currently used for HalaqaSupervisors).
    /// </summary>
    public class UpdateUserDto
    {
        [Required(ErrorMessage = "الاسم الكامل مطلوب")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "رقم الجوال مطلوب")]
        [SaudiPhoneNumber(ErrorMessage = "رقم الجوال يجب أن يكون سعودي ويبدأ بـ +966 5")]
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
