using System;
using System.ComponentModel.DataAnnotations;

namespace Emaily.BLL.Attributes
{
    public class NotEmptyGuidAttribute : ValidationAttribute
    {
        public NotEmptyGuidAttribute()
        {
            ErrorMessage = "The provided GUID is invalid or empty.";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is Guid guid && guid == Guid.Empty)
            {
                return new ValidationResult(ErrorMessage);
            }
            return ValidationResult.Success;
        }
    }
}