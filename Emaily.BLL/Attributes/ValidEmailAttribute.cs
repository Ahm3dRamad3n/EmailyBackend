using System.ComponentModel.DataAnnotations;

namespace Emaily.BLL.Attributes
{
    public class ValidEmailAttribute : ValidationAttribute
    {
        private readonly bool _allowNull;

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
                // التحقق من صيغة الإيميل باستخدام كلاس .NET الجاهز
                var emailValidator = new EmailAddressAttribute();
                if (!emailValidator.IsValid(singleEmail))
                {
                    return new ValidationResult($"Invalid email format: {singleEmail}");
                }
            }

            return ValidationResult.Success;
        }
    }
}