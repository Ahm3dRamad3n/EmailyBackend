using System.ComponentModel.DataAnnotations;

namespace Emaily.BLL.Attributes
{
    public class RequiredTextAttribute : ValidationAttribute
    {
        public RequiredTextAttribute()
        {
            ErrorMessage = "This field is required and cannot be empty or whitespace only.";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not string text || string.IsNullOrWhiteSpace(text))
            {
                return new ValidationResult(ErrorMessage);
            }

            return ValidationResult.Success;
        }
    }
}