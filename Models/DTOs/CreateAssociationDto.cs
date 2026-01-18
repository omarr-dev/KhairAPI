using System.ComponentModel.DataAnnotations;
using KhairAPI.Core.Attributes;

namespace KhairAPI.Models.DTOs
{
    /// <summary>
    /// DTO for creating a new association from the public landing page.
    /// Creates both the association and an initial supervisor user.
    /// </summary>
    public class CreateAssociationDto
    {
        // Association Information
        [Required(ErrorMessage = "اسم الجمعية مطلوب")]
        [MaxLength(255, ErrorMessage = "اسم الجمعية يجب ألا يتجاوز 255 حرف")]
        public string AssociationName { get; set; } = string.Empty;

        [Required(ErrorMessage = "الاسم الفرعي للرابط مطلوب")]
        [MaxLength(100, ErrorMessage = "الاسم الفرعي يجب ألا يتجاوز 100 حرف")]
        [RegularExpression(@"^[a-zA-Z0-9\-]+$", ErrorMessage = "الاسم الفرعي يجب أن يحتوي على أحرف انجليزية وأرقام وشرطات فقط")]
        public string Subdomain { get; set; } = string.Empty;

        [MaxLength(1000, ErrorMessage = "الوصف يجب ألا يتجاوز 1000 حرف")]
        public string? Description { get; set; }

        [MaxLength(100, ErrorMessage = "اسم الدولة يجب ألا يتجاوز 100 حرف")]
        public string? Country { get; set; }

        [MaxLength(100, ErrorMessage = "اسم المدينة يجب ألا يتجاوز 100 حرف")]
        public string? City { get; set; }

        // Admin/Manager Information
        [Required(ErrorMessage = "اسم المسؤول مطلوب")]
        [MaxLength(255, ErrorMessage = "اسم المسؤول يجب ألا يتجاوز 255 حرف")]
        public string ManagerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "رقم جوال المسؤول مطلوب")]
        [SaudiPhoneNumber(ErrorMessage = "رقم الجوال يجب أن يكون سعودي ويبدأ بـ +966 5")]
        public string ManagerPhoneNumber { get; set; } = string.Empty;

        [MaxLength(255, ErrorMessage = "البريد الإلكتروني يجب ألا يتجاوز 255 حرف")]
        [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صحيح")]
        public string? Email { get; set; }
    }

    /// <summary>
    /// Response DTO after successfully creating an association.
    /// </summary>
    public class CreateAssociationResponseDto
    {
        public int AssociationId { get; set; }
        public string AssociationName { get; set; } = string.Empty;
        public string Subdomain { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        
        // The login URL for the new association
        public string LoginUrl { get; set; } = string.Empty;
    }
}
