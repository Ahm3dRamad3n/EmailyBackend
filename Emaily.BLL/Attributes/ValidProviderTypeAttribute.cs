using System.ComponentModel.DataAnnotations;
using System.Linq;
using static Emaily.DAL.Entities.Service;

namespace Emaily.BLL.Attributes
{
    public class ValidProviderTypeAttribute : ValidationAttribute
    {
        private readonly string[] _allowedTypes =
        [
            ProviderTypes.AppPassword,
            ProviderTypes.ApiKey,
            ProviderTypes.OAuth
        ];

        public ValidProviderTypeAttribute()
        {
            ErrorMessage = "Invalid provider type. Allowed values are: AppPassword, ApiKey, OAuth.";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not string type || string.IsNullOrWhiteSpace(type))
            {
                return new ValidationResult("Provider type cannot be empty.");
            }

            if (!_allowedTypes.Contains(type))
            {
                return new ValidationResult(ErrorMessage);
            }

            return ValidationResult.Success;
        }
    }
}