using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Emaily.BLL.Attributes
{
    public partial class ValidEmailAttribute : ValidationAttribute
    {
        private readonly bool _allowNull;

        // تعبير نمطي صارم يقبل فقط الإيميلات الصافية بدون مسافات أو أسماء مدمجة
        private static readonly Regex StrictEmailRegex = GenerateStrictEmailRegex();

        public ValidEmailAttribute(bool AllowNull = false)
        {
            _allowNull = AllowNull;
            ErrorMessage = "Email address cannot be empty.";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return _allowNull ? ValidationResult.Success : new ValidationResult(ErrorMessage);
            }

            if (value is not string email || string.IsNullOrWhiteSpace(email))
            {
                return new ValidationResult(ErrorMessage);
            }

            var emails = email.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var singleEmail in emails)
            {
                if (!StrictEmailRegex.IsMatch(singleEmail))
                {
                    return new ValidationResult($"Invalid email format: {singleEmail}");
                }
            }

            return ValidationResult.Success;
        }

        [GeneratedRegex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", RegexOptions.Compiled)]
        private static partial Regex GenerateStrictEmailRegex();
    }
}