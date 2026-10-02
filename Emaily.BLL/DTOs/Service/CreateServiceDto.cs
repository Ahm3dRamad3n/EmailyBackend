using Emaily.BLL.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Emaily.BLL.DTOs.Service
{
    public class CreateServiceDto
    {
        [ValidProviderType]
        public string ProviderType { get; set; } = null!;

        [ValidEmail]
        public string FromEmail { get; set; } = null!;

        [RequiredText]
        public string FromName { get; set; } = null!;

        public ServiceApiKeyDto? ServiceApiKey { get; set; }
        public ServiceOauthDto? ServiceOauth { get; set; }
        public ServiceAppPasswordDto? ServiceAppPassword { get; set; }

    }
}