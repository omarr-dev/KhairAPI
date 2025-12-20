using System.ComponentModel.DataAnnotations;
using KhairAPI.Core.Helpers;

namespace KhairAPI.Core.Attributes
{
    /// <summary>
    /// Validates that a phone number is a valid Saudi phone number
    /// </summary>
    public class SaudiPhoneNumberAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                // If field is required, use [Required] attribute separately
                return ValidationResult.Success;
            }

            var phoneNumber = value.ToString()!;

            if (!PhoneNumberValidator.IsValid(phoneNumber))
            {
                return new ValidationResult(PhoneNumberValidator.GetValidationErrorMessage());
            }

            return ValidationResult.Success;
        }
    }
}
