using System.ComponentModel.DataAnnotations;

namespace KhairAPI.Models.DTOs
{
    public class LoginDto
    {
        // Accepts a Saudi phone number (+9665XXXXXXXX / 05XXXXXXXX) OR a National ID.
        // Imported teachers log in with their National ID until they set a real phone.
        // Validation of the actual format happens in AuthService so a NID isn't rejected here.
        [Required(ErrorMessage = "رقم الجوال أو الهوية مطلوب")]
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
