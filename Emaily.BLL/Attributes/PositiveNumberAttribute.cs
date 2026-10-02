using System.ComponentModel.DataAnnotations;

namespace Emaily.BLL.Attributes
{
    public class PositiveNumberAttribute : ValidationAttribute
    {
        public PositiveNumberAttribute()
        {
            ErrorMessage = "The number must be 0 or greater.";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is int intValue && intValue < 0) return new ValidationResult(ErrorMessage);
            if (value is double doubleValue && doubleValue < 0) return new ValidationResult(ErrorMessage);
            if (value is decimal decimalValue && decimalValue < 0) return new ValidationResult(ErrorMessage);

            return ValidationResult.Success;
        }
    }
}