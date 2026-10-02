using System;
using System.ComponentModel.DataAnnotations;

namespace Emaily.BLL.Attributes
{
    public class FutureDateAttribute : ValidationAttribute
    {
        public FutureDateAttribute()
        {
            ErrorMessage = "The date must be today or in the future.";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is DateTime date)
            {
                if (date.Date < DateTime.UtcNow.Date)
                {
                    return new ValidationResult(ErrorMessage);
                }
            }
            return ValidationResult.Success;
        }
    }
}