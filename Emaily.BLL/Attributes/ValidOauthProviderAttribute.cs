using System.ComponentModel.DataAnnotations;
using System.Linq;
using static Emaily.DAL.Entities.ServiceOauth;

namespace Emaily.BLL.Attributes
{
    public class ValidOauthProviderAttribute : ValidationAttribute
    {
        private readonly string[] _allowedProviders =
        [
            OauthProviders.Google,
            OauthProviders.Microsoft
        ];

        public ValidOauthProviderAttribute()
        {
            ErrorMessage = "Invalid OAuth provider. Allowed values are: Google, Microsoft.";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not string provider || string.IsNullOrWhiteSpace(provider))
            {
                return new ValidationResult("OAuth provider cannot be empty.");
            }

            if (!_allowedProviders.Contains(provider))
            {
                return new ValidationResult(ErrorMessage);
            }

            return ValidationResult.Success;
        }
    }
}