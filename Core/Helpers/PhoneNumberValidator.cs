using System.Linq;
using System.Text.RegularExpressions;

namespace KhairAPI.Core.Helpers
{
    /// <summary>
    /// Validator for Saudi phone numbers
    /// </summary>
    public static class PhoneNumberValidator
    {
        // Valid Saudi mobile prefixes (50, 53, 54, 55, 56, 57, 58, 59)
        private static readonly string[] ValidPrefixes = { "50", "53", "54", "55", "56", "57", "58", "59" };

        // Regex pattern: +966 followed by 5 and 8 more digits
        private const string PhonePattern = @"^\+9665[0-9]{8}$";

        /// <summary>
        /// Validates Saudi phone number format
        /// </summary>
        /// <param name="phoneNumber">Phone number to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValid(string? phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            // Remove all spaces and special characters except +
            var cleaned = CleanPhoneNumber(phoneNumber);

            // Check basic format
            if (!Regex.IsMatch(cleaned, PhonePattern))
                return false;

            // Extract prefix (characters 4-5, after +966)
            var prefix = cleaned.Substring(4, 2);

            // Validate prefix is in allowed list
            return ValidPrefixes.Contains(prefix);
        }

        /// <summary>
        /// Formats phone number to standard format: +966XXXXXXXXX
        /// </summary>
        /// <param name="phoneNumber">Input phone number</param>
        /// <returns>Formatted phone number or null if invalid</returns>
        public static string? Format(string? phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return null;

            var cleaned = CleanPhoneNumber(phoneNumber);

            // Add +966 if missing
            if (!cleaned.StartsWith("+966"))
            {
                // Handle cases like 05XXXXXXXX or 5XXXXXXXX
                if (cleaned.StartsWith("0"))
                    cleaned = "+966" + cleaned.Substring(1);
                else if (cleaned.StartsWith("5"))
                    cleaned = "+966" + cleaned;
                else if (cleaned.StartsWith("966"))
                    cleaned = "+" + cleaned;
                else
                    return null;
            }

            // Validate and return
            return IsValid(cleaned) ? cleaned : null;
        }

        /// <summary>
        /// Removes all spaces, dashes, and parentheses from phone number
        /// </summary>
        private static string CleanPhoneNumber(string phoneNumber)
        {
            return phoneNumber.Replace(" ", "")
                             .Replace("-", "")
                             .Replace("(", "")
                             .Replace(")", "")
                             .Trim();
        }

        /// <summary>
        /// Gets error message for invalid phone number
        /// </summary>
        public static string GetValidationErrorMessage()
        {
            return "رقم الجوال يجب أن يكون سعودي ويبدأ بـ +966 5";
        }
    }
}
