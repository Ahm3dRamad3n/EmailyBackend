using System;
using System.ComponentModel.DataAnnotations;

namespace Emaily.BLL.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class RequiredTextAttribute : ValidationAttribute
    {
        private string[]? _allowedValues = null;

        public RequiredTextAttribute(params string[]? allowedValues)
        {
            _allowedValues = allowedValues;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return new ValidationResult("The field is required.");
            }

            if (_allowedValues != null && _allowedValues.Length > 0)
            {
                if (!_allowedValues.Contains(value.ToString()))
                {
                    return new ValidationResult("The value is not allowed.");
                }
            }

            return ValidationResult.Success;
        }
    }
}