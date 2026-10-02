using System.ComponentModel.DataAnnotations;
using System.Linq;
using static Emaily.DAL.Entities.Project;

namespace Emaily.BLL.Attributes
{
    public class ValidAccessModeAttribute : ValidationAttribute
    {
        private readonly string[] _allowedModes =
        [
            AccessModes.FrontendOnly,
            AccessModes.BackendOnly,
            AccessModes.Hybrid
        ];

        public ValidAccessModeAttribute()
        {
            ErrorMessage = "Invalid access mode. Allowed values are: FrontendOnly, BackendOnly, Hybrid.";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not string mode || string.IsNullOrWhiteSpace(mode))
            {
                return new ValidationResult("Access mode cannot be empty.");
            }

            if (!_allowedModes.Contains(mode))
            {
                return new ValidationResult(ErrorMessage);
            }

            return ValidationResult.Success;
        }
    }
}