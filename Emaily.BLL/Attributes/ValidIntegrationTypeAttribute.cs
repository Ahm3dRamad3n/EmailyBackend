using System.ComponentModel.DataAnnotations;
using System.Linq;
using static Emaily.DAL.Entities.Integration;

namespace Emaily.BLL.Attributes
{
    public class ValidIntegrationTypeAttribute : ValidationAttribute
    {
        private readonly string[] _allowedTypes =
        [
            IntegrationTypes.TelegramBot,
            IntegrationTypes.GoogleSheets,
            IntegrationTypes.AiSummary
        ];

        public ValidIntegrationTypeAttribute()
        {
            ErrorMessage = "Invalid integration type. Allowed values are: TelegramBot, GoogleSheets, AiSummary.";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not string type || string.IsNullOrWhiteSpace(type))
            {
                return new ValidationResult("Integration type cannot be empty.");
            }

            if (!_allowedTypes.Contains(type))
            {
                return new ValidationResult(ErrorMessage);
            }

            return ValidationResult.Success;
        }
    }
}