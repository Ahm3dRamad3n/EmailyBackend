namespace Emaily.BLL.DTOs.Service
{
    public class ServiceDto
    {
        public string Id { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public string ProviderType { get; set; } = null!;
        public string FromEmail { get; set; } = null!;
        public string FromName { get; set; } = null!;
        public ServiceApiKeyDto? ServiceApiKey { get; set; }
        public ServiceOauthDto? ServiceOauth { get; set; }
        public ServiceAppPasswordDto? ServiceAppPassword { get; set; }
        public bool IsActive { get; set; }
        public bool IsLocked { get; set; }
    }

    public partial class ServiceApiKeyDto
    {
        public string ProviderName { get; set; } = null!; // e.g., "SendGrid", "Resend"
        public string SecretApiKey { get; set; } = null!;
    }

    public partial class ServiceOauthDto
    {
        public string OauthProvider { get; set; } = null!; // e.g., "Google", "Microsoft"

        public bool IsOAuthConnected { get; set; }

        public string AccessToken { get; set; } = null!;

        public string RefreshToken { get; set; } = null!;

        public DateTime TokenExpiry { get; set; }
    }

    public partial class ServiceAppPasswordDto
    {

        public string SmtpHost { get; set; } = null!;

        public int SmtpPort { get; set; }

        public string Username { get; set; } = null!;

        public string Password { get; set; } = null!;
    }
}