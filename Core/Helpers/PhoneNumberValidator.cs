using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace KhairAPI.Core.Helpers
{
    /// <summary>
    /// Validator for Saudi phone numbers.
    /// Accepts any common input form (05XXXXXXXX, 5XXXXXXXX, 9665XXXXXXXX,
    /// 009665XXXXXXXX, +9665XXXXXXXX) and normalizes to +9665XXXXXXXX.
    /// </summary>
    public static class PhoneNumberValidator
    {
        // Valid Saudi mobile prefixes (50, 53, 54, 55, 56, 57, 58, 59)
        private static readonly string[] ValidPrefixes = { "50", "53", "54", "55", "56", "57", "58", "59" };

        // Normalized format: +966 followed by 5 and 8 more digits
        private const string PhonePattern = @"^\+9665[0-9]{8}$";

        /// <summary>
        /// Validates that the input is a Saudi mobile number in ANY accepted form.
        /// </summary>
        public static bool IsValid(string? phoneNumber) => Normalize(phoneNumber) != null;

        /// <summary>
        /// Normalizes a phone number to the standard stored format: +9665XXXXXXXX.
        /// Returns null if the input is not a valid Saudi mobile number.
        /// </summary>
        public static string? Format(string? phoneNumber) => Normalize(phoneNumber);

        /// <summary>
        /// Core normalization: converts any accepted form to +9665XXXXXXXX,
        /// or returns null if invalid.
        /// </summary>
        private static string? Normalize(string? phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return null;

            var cleaned = CleanPhoneNumber(phoneNumber);

            // Unify any country-code form to "+966..."
            if (cleaned.StartsWith("+966"))
            {
                // already prefixed
            }
            else if (cleaned.StartsWith("00966"))
                cleaned = "+" + cleaned.Substring(2);
            else if (cleaned.StartsWith("966"))
                cleaned = "+" + cleaned;
            else if (cleaned.StartsWith("0"))
                cleaned = "+966" + cleaned.Substring(1);
            else if (cleaned.StartsWith("5"))
                cleaned = "+966" + cleaned;
            else
                return null;

            if (!Regex.IsMatch(cleaned, PhonePattern))
                return null;

            // Prefix is the two digits right after "+966"
            var prefix = cleaned.Substring(4, 2);
            return ValidPrefixes.Contains(prefix) ? cleaned : null;
        }

        /// <summary>
        /// Converts Arabic-Indic digits to Western digits and removes every
        /// character except digits and a leading '+'.
        /// </summary>
        private static string CleanPhoneNumber(string phoneNumber)
        {
            var sb = new StringBuilder(phoneNumber.Length);
            foreach (var ch in phoneNumber.Trim())
            {
                if (ch >= '٠' && ch <= '٩') // Arabic-Indic ٠-٩
                    sb.Append((char)('0' + (ch - '٠')));
                else if (ch >= '۰' && ch <= '۹') // Extended (Persian) ۰-۹
                    sb.Append((char)('0' + (ch - '۰')));
                else if (char.IsDigit(ch) || ch == '+')
                    sb.Append(ch);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Gets error message for invalid phone number.
        /// </summary>
        public static string GetValidationErrorMessage()
        {
            return "رقم الجوال يجب أن يكون رقم جوال سعودي صحيح (مثال: 0501234567)";
        }
    }
}
