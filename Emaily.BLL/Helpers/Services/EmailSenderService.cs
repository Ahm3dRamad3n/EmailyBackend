using Emaily.BLL.DTOs;
using Emaily.DAL;
using Emaily.DAL.Entities;
using Emaily.DAL.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MimeKit;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Emaily.BLL.Helpers;
using Emaily.BLL.Helpers.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Emaily.BLL.Helpers.Services
{
    public class EmailSenderService : IEmailSenderService
    {
        private readonly IUnitOfWork _uow;
        private readonly HttpClient _httpClient; // لإرسال طلبات الـ APIs الخارجية
        private readonly ILoggerService _logger;
        private readonly IEncryptionHelper _encryptionHelper;
        private readonly IAttachmentManager _attachmentManager;

        private readonly struct EmailConfig
        {
            public string SmtpHost { get; init; }
            public int SmtpPort { get; init; }
            public string FromEmail { get; init; }
            public string FromName { get; init; }
            public string Username { get; init; }
            public string DecryptedPassword { get; init; }
        }
        private readonly EmailConfig _emailConfig;

        private readonly string _adminEmail;

        private readonly struct OAuthConfig
        {
            public string ClientId { get; init; }
            public string ClientSecret { get; init; }
            public string TokenEndpoint { get; init; }
        }
        private readonly OAuthConfig _googleOAuthConfig;
        private readonly OAuthConfig _microsoftOAuthConfig;

        public EmailSenderService(IUnitOfWork uow, HttpClient httpClient, ILoggerService logger, IConfiguration configuration, IEncryptionHelper encryptionHelper, IAttachmentManager attachmentManager)
        {
            _uow = uow;
            _httpClient = httpClient;
            _logger = logger;
            _encryptionHelper = encryptionHelper;
            _attachmentManager = attachmentManager;

            string smtpHost = configuration["SystemEmail:SmtpHost"] ?? throw new Exception("SMTP host is not configured.");
            int smtpPort = int.TryParse(configuration["SystemEmail:SmtpPort"], out var port) ? port : throw new Exception("SMTP port is not configured.");
            string FromEmail = configuration["SystemEmail:FromEmail"] ?? throw new Exception("System email is not configured.");
            string FromName = configuration["SystemEmail:FromName"] ?? "Emaily System";
            string username = configuration["SYSTEM_SMTP_USERNAME"] ?? throw new Exception("SMTP username is not configured.");
            string encryptedPassword = configuration["SYSTEM_SMTP_ENCRYPTED_PASSWORD"] ?? throw new Exception("SMTP encrypted password is not configured.");
            string decryptedPassword = _encryptionHelper.Decrypt(encryptedPassword);

            _emailConfig = new EmailConfig
            {
                SmtpHost = smtpHost,
                SmtpPort = smtpPort,
                FromEmail = FromEmail,
                FromName = FromName,
                Username = username,
                DecryptedPassword = decryptedPassword
            };

            _adminEmail = configuration["ADMIN_EMAIL"] ?? throw new Exception("Admin email is not configured.");

            _googleOAuthConfig = new OAuthConfig
            {
                ClientId = configuration["GOOGLE_CLIENT_ID"] ?? throw new Exception("Google Client ID is not configured."),
                ClientSecret = configuration["GOOGLE_CLIENT_SECRET"] ?? throw new Exception("Google Client Secret is not configured."),
                TokenEndpoint = "https://oauth2.googleapis.com/token"
            };
            _microsoftOAuthConfig = new OAuthConfig
            {
                ClientId = configuration["MICROSOFT_CLIENT_ID"] ?? throw new Exception("Microsoft Client ID is not configured."),
                ClientSecret = configuration["MICROSOFT_CLIENT_SECRET"] ?? throw new Exception("Microsoft Client Secret is not configured."),
                TokenEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/token"
            };
        }

        public async Task<SendEmailResponseDto> NotifyAdminAsync(string subject, string body, string? replyTo, CancellationToken cancellationToken, FileDto? dto = null)
        {
            IFormFile? attachment = _attachmentManager.ConvertToIFormFile(dto);
            return await SendWithSystemAsync("Emaily System", _adminEmail, subject, body, cancellationToken, new SendEmailDto 
             {
                 ReplyTo = replyTo,
                 Attachments = attachment != null ? [attachment] : []
             });
        }

        public async Task<SendEmailResponseDto> SendWithSystemAsync(string fullName, string toEmail, string subject, string body, CancellationToken cancellationToken, SendEmailDto? dto = null)
        {
            var emailMessage = await CreateMimeMessage(_emailConfig.FromName, _emailConfig.FromEmail, fullName, toEmail, subject, body, dto);
            using var smtpClient = new SmtpClient();
            await smtpClient.ConnectAsync(_emailConfig.SmtpHost, _emailConfig.SmtpPort, SecureSocketOptions.StartTls);
            await smtpClient.AuthenticateAsync(_emailConfig.Username, _emailConfig.DecryptedPassword);
            await smtpClient.SendAsync(emailMessage);
            await smtpClient.DisconnectAsync(true);

            return new SendEmailResponseDto
            {
                Success = true
            };
        }
        
        public async Task<SendEmailResponseDto> SendEmailAsync(SendEmailDto dto)
        {
            // اثق ان كل المدخلات صحيحة حيث ثم التحقق من صحتها في الـ Controller أو Service الأعلى.

            
            var serviceProvider = await _uow.Services.FindAsync(s => s.Id == dto.ServiceId && s.IsActive && !s.IsDeleted,
                includes: sp => sp.Include(s => s.ServiceAppPassword)
                                  .Include(s => s.ServiceApiKey)
                                  .Include(s => s.ServiceOauth));
            if (serviceProvider == null)
            {
                var serviceProviderId = _uow.ProjectServices.SelectWhereAsync(selector: sp => sp.ServiceId, criteria: sp => dto.ServiceId == sp.ServiceId && sp.ProjectId == dto.ProjectId && !sp.Service.IsDeleted);
                serviceProvider = await _uow.Services.FindAsync(sp => sp.Id == serviceProviderId.ToString(),
                    includes: sp => sp.Include(s => s.ServiceAppPassword)
                                      .Include(s => s.ServiceApiKey)
                                      .Include(s => s.ServiceOauth));
                if (serviceProvider == null)
                {
                    return new SendEmailResponseDto {
                        Success = false,
                        ErrorMessage = "Could not find a valid service provider for the given template and project."
                    };
                }
            }

            if (serviceProvider.IsLocked)
            {
                return new SendEmailResponseDto
                {
                    Success = false,
                    ErrorMessage = "The selected service provider is currently locked"
                };
            }

            // 4. توجيه الإرسال وتمرير البيانات النهائية
            return serviceProvider.ProviderType switch
            {
                nameof(Service.ProviderTypes.AppPassword) => await SendViaSmtpAppPasswordAsync(serviceProvider, dto),
                nameof(Service.ProviderTypes.ApiKey) => await SendViaExternalApiAsync(serviceProvider, dto),
                nameof(Service.ProviderTypes.OAuth) => await SendViaOAuthAsync(serviceProvider, dto),
                _ => new SendEmailResponseDto
                {
                    Success = false,
                    ErrorMessage = $"Provider type '{serviceProvider.ProviderType}' is invalid."
                }
            };
        }

        private static async Task<MimeMessage> CreateMimeMessage(string FromName, string FromEmail, string? ToName, string toEmail, string Subject, string Body, SendEmailDto? dto = null)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(FromName, FromEmail));
            message.To.Add(new MailboxAddress(ToName ?? "", toEmail));
            message.Subject = Subject;

            if (!string.IsNullOrWhiteSpace(dto?.ReplyTo))
                message.ReplyTo.Add(new MailboxAddress("", dto.ReplyTo));

            if (dto?.Cc != null && dto.Cc.Count > 0)
                foreach (var cc in dto.Cc) message.Cc.Add(new MailboxAddress("", cc));

            if (dto?.Bcc != null && dto.Bcc.Count > 0)
                foreach (var bcc in dto.Bcc) message.Bcc.Add(new MailboxAddress("", bcc));

            var builder = new BodyBuilder { HtmlBody = Body };

            if (dto?.Attachments != null && dto.Attachments.Count > 0)
            {
                foreach (var file in dto.Attachments)
                {
                    if (file.Length > 0)
                    {
                        using var stream = file.OpenReadStream(); // القراءة المباشرة من الـ RAM
                        await builder.Attachments.AddAsync(file.FileName, stream, ContentType.Parse(file.ContentType));
                    }
                }
            }
            message.Body = builder.ToMessageBody();
            return message;
        }

        // --- الطريقة الأولى: App Password / SMTP التقليدي ---
        private async Task<SendEmailResponseDto> SendViaSmtpAppPasswordAsync(Service serviceProvider, SendEmailDto dto)
        {

            var decryptedPassword = _encryptionHelper.Decrypt(serviceProvider.ServiceAppPassword!.EncryptedPassword);
            var emailMessage = await CreateMimeMessage(serviceProvider.FromName, serviceProvider.FromEmail, dto.ToName, dto.ToEmail, dto.Subject, dto.Body, dto);

            try
            {
                using var smtpClient = new SmtpClient();
                await smtpClient.ConnectAsync(serviceProvider.ServiceAppPassword.SmtpHost, serviceProvider.ServiceAppPassword.SmtpPort, SecureSocketOptions.StartTls);
                await smtpClient.AuthenticateAsync(serviceProvider.ServiceAppPassword.Username, decryptedPassword);
                await smtpClient.SendAsync(emailMessage);
                await smtpClient.DisconnectAsync(true);
                
                return new SendEmailResponseDto
                {
                    Success = true,
                    FromName = serviceProvider.FromName,
                    FromEmail = serviceProvider.FromEmail
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(new Log(serviceProvider.UserId, $"SMTP Sending Error: {ex.Message}"));
                return new SendEmailResponseDto
                {
                    Success = false,
                    ErrorMessage = $"SMTP Sending Error: {ex.Message}"
                };
            }
        }
       
        // --- الطريقة الثانية: عبر API (مثل Resend, SendGrid) ---
        private async Task<SendEmailResponseDto> SendViaExternalApiAsync(Service serviceProvider, SendEmailDto dto)
        {
            var decryptedApiKey = _encryptionHelper.Decrypt(serviceProvider.ServiceApiKey!.SecretApiKey);
            var providerName = serviceProvider.ServiceApiKey.ProviderName;

            return providerName switch
            {
                ServiceApiKey.ProviderNames.Resend => await SendWithResendAsync(serviceProvider, dto, decryptedApiKey),
                ServiceApiKey.ProviderNames.SendGrid => await SendWithSendGridAsync(serviceProvider, dto, decryptedApiKey),
                _ => new SendEmailResponseDto
                {
                    Success = false,
                    ErrorMessage = $"API provider '{providerName}' is not supported."
                }
            };
        }
        private async Task<SendEmailResponseDto> SendWithResendAsync(Service serviceProvider, SendEmailDto dto, string apiKey)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            // استخدام Dictionary لمرونة إضافة الحقول فقط إذا كانت موجودة (لتجنب إرسال Nulls)
            var payload = new Dictionary<string, object>
            {
                { "from", $"{serviceProvider.FromName} <{serviceProvider.FromEmail}>" },
                { "to", new[] { dto.ToEmail } },
                { "subject", dto.Subject },
                { "html", dto.Body }
            };

            if (dto.Cc.Count != 0) payload.Add("cc", dto.Cc);
            if (dto.Bcc.Count != 0) payload.Add("bcc", dto.Bcc);
            if (!string.IsNullOrWhiteSpace(dto.ReplyTo)) payload.Add("reply_to", dto.ReplyTo);

            if (dto.Attachments.Count != 0)
            {
                var attachmentsList = new List<object>();
                foreach (var file in dto.Attachments)
                {
                    if (file.Length > 0)
                    {
                        using var ms = new MemoryStream();
                        await file.CopyToAsync(ms);
                        attachmentsList.Add(new
                        {
                            filename = file.FileName,
                            content = Convert.ToBase64String(ms.ToArray())
                        });
                    }
                }
                payload.Add("attachments", attachmentsList);
            }

            var response = await _httpClient.PostAsJsonAsync("https://api.resend.com/emails", payload);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return new SendEmailResponseDto { Success = false, ErrorMessage = $"Resend API Error: {error}" };
            }

            return new SendEmailResponseDto { Success = true, FromName = serviceProvider.FromName, FromEmail = serviceProvider.FromEmail };
        }
        private async Task<SendEmailResponseDto> SendWithSendGridAsync(Service serviceProvider, SendEmailDto dto, string apiKey)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            // 1. تجهيز الـ Personalization (المرسل إليهم والنسخ المخفية والكربونية)
            var personalization = new Dictionary<string, object>
            {
                { "to", new[] { new { email = dto.ToEmail } } }
            };

            if (dto.Cc.Count != 0)
                personalization.Add("cc", dto.Cc.Select(e => new { email = e }).ToArray());

            if (dto.Bcc.Count != 0)
                personalization.Add("bcc", dto.Bcc.Select(e => new { email = e }).ToArray());

            // 2. تجهيز الـ Payload الأساسي
            var payload = new Dictionary<string, object>
            {
                { "personalizations", new[] { personalization } },
                { "from", new { email = serviceProvider.FromEmail, name = serviceProvider.FromName } },
                { "subject", dto.Subject },
                { "content", new[] { new { type = "text/html", value = dto.Body } } }
            };

            // 3. إضافة Reply-To
            if (!string.IsNullOrWhiteSpace(dto.ReplyTo))
                payload.Add("reply_to", new { email = dto.ReplyTo });

            // 4. إضافة المرفقات
            if (dto.Attachments.Count != 0)
            {
                var attachmentsList = new List<object>();
                foreach (var file in dto.Attachments)
                {
                    if (file.Length > 0)
                    {
                        using var ms = new MemoryStream();
                        await file.CopyToAsync(ms);
                        attachmentsList.Add(new
                        {
                            content = Convert.ToBase64String(ms.ToArray()),
                            filename = file.FileName,
                            type = file.ContentType,
                            disposition = "attachment" // مهم جداً لـ SendGrid
                        });
                    }
                }
                payload.Add("attachments", attachmentsList);
            }

            var response = await _httpClient.PostAsJsonAsync("https://api.sendgrid.com/v3/mail/send", payload);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return new SendEmailResponseDto { Success = false, ErrorMessage = $"SendGrid API Error: {error}" };
            }

            return new SendEmailResponseDto { Success = true, FromName = serviceProvider.FromName, FromEmail = serviceProvider.FromEmail };
        }
        // --- الدالة الأساسية للإرسال عبر OAuth ---
        private async Task<SendEmailResponseDto> SendViaOAuthAsync(Service serviceProvider, SendEmailDto dto)
        {
            if (serviceProvider.ServiceOauth == null || string.IsNullOrEmpty(serviceProvider.ServiceOauth.RefreshToken))
            {
                return new SendEmailResponseDto { Success = false, ErrorMessage = "OAuth credentials are missing." };
            }

            if (serviceProvider.ServiceOauth.TokenExpiry <= DateTime.UtcNow.AddMinutes(1))
            {
                var responseDto = await RefreshOAuthTokenAsync(serviceProvider.ServiceOauth);
                if (!responseDto.Success)
                {
                    _logger.LogError(new Log(serviceProvider.UserId, $"Failed to refresh OAuth token: {responseDto.ErrorMessage}"));
                    return new SendEmailResponseDto { Success = false, ErrorMessage = $"Failed to refresh token. {responseDto.ErrorMessage}" };
                }
                _uow.ServiceOauths.Update(serviceProvider.ServiceOauth);
                await _uow.CompleteAsync();
            }

            string accessToken = serviceProvider.ServiceOauth.AccessToken;
            string providerName = serviceProvider.ServiceOauth.OauthProvider;
            string fromEmail = serviceProvider.FromEmail.ToLower();

            var message = await CreateMimeMessage(serviceProvider.FromName, fromEmail, dto.ToName, dto.ToEmail, dto.Subject, dto.Body, dto);

            try
            {
                if (providerName == ServiceOauth.OauthProviders.Google)
                {
                    // استخدام Gmail API بدلاً من SMTP لتعمل مع صلاحية gmail.send الآمنة
                    using var memoryStream = new MemoryStream();
                    await message.WriteToAsync(memoryStream);

                    // تحويل الرسالة لصيغة Base64Url التي تطلبها جوجل
                    string rawMessage = Convert.ToBase64String(memoryStream.ToArray())
                        .Replace('+', '-')
                        .Replace('/', '_')
                        .Replace("=", "");

                    using var client = new HttpClient();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                    var payload = new { raw = rawMessage };

                    var response = await client.PostAsJsonAsync("https://gmail.googleapis.com/gmail/v1/users/me/messages/send", payload);

                    if (!response.IsSuccessStatusCode)
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        return new SendEmailResponseDto { Success = false, ErrorMessage = $"Gmail API Error: {errorContent}" };
                    }
                }
                else if (providerName == ServiceOauth.OauthProviders.Microsoft)
                {
                    // استخدام Microsoft Graph API بدلاً من SMTP لتعمل مع سكوب Mail.Send
                    using var memoryStream = new MemoryStream();
                    await message.WriteToAsync(memoryStream);

                    // مايكروسوفت تطلب Base64 عادي (لا نحتاج لتبديل الرموز مثل جوجل)
                    string rawMessage = Convert.ToBase64String(memoryStream.ToArray());

                    using var client = new HttpClient();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                    // Graph API يتوقع أن نرسل الـ Base64 كنص عادي (text/plain)
                    var content = new StringContent(rawMessage, System.Text.Encoding.UTF8, "text/plain");

                    // الإرسال مباشرة عبر الـ EndPoint الخاص بـ مايكروسوفت
                    var response = await client.PostAsync("https://graph.microsoft.com/v1.0/me/sendMail", content);

                    if (!response.IsSuccessStatusCode)
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        return new SendEmailResponseDto { Success = false, ErrorMessage = $"Microsoft Graph API Error: {errorContent}" };
                    }
                }
                else
                {
                    _logger.LogCritical(new Log(serviceProvider.UserId, $"Unsupported OAuth provider: {providerName}"));
                    return new SendEmailResponseDto { Success = false, ErrorMessage = $"Provider '{providerName}' is not supported." };
                }

                return new SendEmailResponseDto
                {
                    Success = true,
                    FromName = serviceProvider.FromName,
                    FromEmail = serviceProvider.FromEmail
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(new Log(serviceProvider.UserId, $"OAuth Sending Error: {ex.Message}"));
                return new SendEmailResponseDto
                {
                    Success = false,
                    ErrorMessage = $"OAuth Sending Error: {ex.Message}"
                };
            }
        }  
        
        // --- دالة مساعدة لتجديد الـ Access Token ---
        private async Task<SendEmailResponseDto> RefreshOAuthTokenAsync(ServiceOauth oauthData)
        {
            string providerName = oauthData.OauthProvider;
            string tokenEndpoint;
            string clientId;
            string clientSecret;

            switch (providerName)
            {
                case ServiceOauth.OauthProviders.Microsoft:
                    tokenEndpoint = _microsoftOAuthConfig.TokenEndpoint;
                    clientId = _microsoftOAuthConfig.ClientId;
                    clientSecret = _microsoftOAuthConfig.ClientSecret;
                    break;
                case ServiceOauth.OauthProviders.Google:
                    tokenEndpoint = _googleOAuthConfig.TokenEndpoint;
                    clientId = _googleOAuthConfig.ClientId;
                    clientSecret = _googleOAuthConfig.ClientSecret;
                    break;
                default:
                    return new SendEmailResponseDto { Success = false, ErrorMessage = $"Provider '{providerName}' not supported." };
            }

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                string errorMsg = $"Missing client ID or secret in appsettings for {providerName} OAuth.";
                _logger.LogCritical(new Log(Guid.Empty, errorMsg));
                return new SendEmailResponseDto { Success = false, ErrorMessage = errorMsg };
            }

            using var httpClient = new HttpClient();
            var requestData = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("refresh_token", oauthData.RefreshToken),
                new KeyValuePair<string, string>("grant_type", "refresh_token")
            ]);

            var response = await httpClient.PostAsync(tokenEndpoint, requestData);
            var jsonResponse = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new SendEmailResponseDto
                {
                    Success = false,
                    ErrorMessage = $"Refresh Token Failed ({response.StatusCode}): {jsonResponse}"
                };
            }

            using var doc = JsonDocument.Parse(jsonResponse);

            if (doc.RootElement.TryGetProperty("access_token", out var accessTokenElement))
            {
                oauthData.AccessToken = accessTokenElement.GetString()!;

                if (doc.RootElement.TryGetProperty("expires_in", out var expiresInElement))
                {
                    oauthData.TokenExpiry = DateTime.UtcNow.AddSeconds(expiresInElement.GetInt32());
                }

                return new SendEmailResponseDto { Success = true };
            }

            return new SendEmailResponseDto { Success = false, ErrorMessage = "Result did not contain access_token." };
        }
    }
}