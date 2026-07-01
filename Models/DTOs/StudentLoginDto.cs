using System.ComponentModel.DataAnnotations;

namespace KhairAPI.Models.DTOs
{
    /// <summary>
    /// Student self-service login. The student is identified by their National ID (NID)
    /// within the current tenant (resolved from the X-Tenant-Id header).
    /// </summary>
    public class StudentLoginDto
    {
        [Required(ErrorMessage = "رقم الهوية الوطنية مطلوب")]
        public string NationalId { get; set; } = string.Empty;
    }
}
