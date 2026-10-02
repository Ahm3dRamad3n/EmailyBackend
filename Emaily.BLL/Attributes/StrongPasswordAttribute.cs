using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Emaily.BLL.Attributes
{
    public partial class StrongPasswordAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not string password || string.IsNullOrWhiteSpace(password))
            {
                return new ValidationResult("Password cannot be empty.");
            }

            if (password.Length < 8)
            {
                return new ValidationResult("Password must be at least 8 characters long.");
            }

            // 3. التحقق من وجود حرف كبير (A-Z)
            if (!Uppercase().IsMatch(password))
            {
                return new ValidationResult("Password must contain at least one uppercase letter (A-Z).");
            }

            // 4. التحقق من وجود حرف صغير (a-z)
            if (!Lowercase().IsMatch(password))
            {
                return new ValidationResult("Password must contain at least one lowercase letter (a-z).");
            }

            // 5. التحقق من وجود رقم (0-9)
            if (!Digit().IsMatch(password))
            {
                return new ValidationResult("Password must contain at least one number (0-9).");
            }

            // 6. التحقق من وجود رمز خاص (مثل * أو @ أو #)
            // \W تعني أي شيء ليس حرفاً أو رقماً
            if (!SpecialCharacter().IsMatch(password))
            {
                return new ValidationResult("Password must contain at least one special character (e.g., *!@#).");
            }

            return ValidationResult.Success;
        }

        [GeneratedRegex(@"[A-Z]")]
        private static partial Regex Uppercase();

        [GeneratedRegex(@"[a-z]")]
        private static partial Regex Lowercase();

        [GeneratedRegex(@"\d")]
        private static partial Regex Digit();

        [GeneratedRegex(@"[\W_]")]
        private static partial Regex SpecialCharacter();
    }
}