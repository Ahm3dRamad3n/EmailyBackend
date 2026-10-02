using Emaily.DAL.Entities;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Emaily.BLL.Attributes
{
    public class ValidApiKeyProviderAttribute : ValidationAttribute
    {
        private readonly string[] _allowedProviders =
        [
            ServiceApiKey.ProviderNames.SendGrid,
            ServiceApiKey.ProviderNames.Resend
        ];

        public ValidApiKeyProviderAttribute()
        {
            ErrorMessage = "Invalid API Key provider. Allowed values are: SendGrid, Resend.";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not string provider || string.IsNullOrWhiteSpace(provider))
            {
                return new ValidationResult("API Key provider cannot be empty.");
            }

            if (!_allowedProviders.Contains(provider))
            {
                return new ValidationResult(ErrorMessage);
            }

            return ValidationResult.Success;
        }
    }
}