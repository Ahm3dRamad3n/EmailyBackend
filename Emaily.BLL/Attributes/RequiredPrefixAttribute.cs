using System.ComponentModel.DataAnnotations;

namespace Emaily.BLL.Attributes
{
    public class RequiredPrefixAttribute : ValidationAttribute
    {
        private readonly string _prefix;
        private readonly bool _allowNull;

        public RequiredPrefixAttribute(string prefix, bool AllowNull = false)
        {
            _prefix = prefix;
            _allowNull = AllowNull;
            ErrorMessage = $"This field must start with the prefix '{_prefix}'.";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return _allowNull ? ValidationResult.Success : new ValidationResult(ErrorMessage);
            }

            if (value is string text)
            {
                if (!text.StartsWith(_prefix))
                {
                    return new ValidationResult(ErrorMessage);
                }
            }
            return ValidationResult.Success;
        }
    }
}