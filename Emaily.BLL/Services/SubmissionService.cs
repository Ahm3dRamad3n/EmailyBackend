using Azure;
using Emaily.BLL.Attributes;
using Emaily.BLL.DTOs;
using Emaily.BLL.DTOs.Submission;
using Emaily.BLL.DTOs.Template;
using Emaily.BLL.Helpers;
using Emaily.BLL.Helpers.Interfaces;
using Emaily.BLL.Interfaces;
using Emaily.DAL.Entities;
using Emaily.DAL.Interfaces;
using Ganss.Xss;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Hangfire;
using Hangfire.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Emaily.BLL.Services
{
    public partial class SubmissionService : ISubmissionService
    {
        private readonly IUnitOfWork _uow;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IEmailSenderService _emailSenderService;
        private readonly ILoggerService _logger;
        private readonly IAttachmentManager _am;
        private readonly int _pageSize;
        private readonly string _jsonCredentials;
        private readonly string _botToken;
        private readonly dynamic[] _aiProviders;


        public SubmissionService(IUnitOfWork uow, IEmailSenderService emailSenderService, IConfiguration configuration, IHttpClientFactory httpClientFactory, ILoggerService logger, IBackgroundJobClient backgroundJobClient, IAttachmentManager attachmentManager)
        {
            _uow = uow;
            _httpClientFactory = httpClientFactory;
            _emailSenderService = emailSenderService;
            _logger = logger;
            _backgroundJobClient = backgroundJobClient;
            _am = attachmentManager;
            _pageSize = configuration.GetValue<int>("PageSize");
            _jsonCredentials = configuration["GOOGLE_SHEETS_CREDENTIALS"] ?? throw new InvalidOperationException("GOOGLE_SHEETS_CREDENTIALS environment variable is not set.");
            _botToken = configuration["TELEGRAM_BOT_TOKEN"] ?? throw new InvalidOperationException("TELEGRAM_BOT_TOKEN environment variable is not set.");


            _aiProviders =
            [
                new
                {
                    Name = "Groq",
                    Url = "https://api.groq.com/openai/v1/chat/completions",
                    Model = "qwen/qwen3.8-27b",
                    ApiKey = configuration["AiSettings:GroqKey"]
                },
                new
                {
                    Name = "Gemini",
                    Url = "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions",
                    Model = "gemini-flash-latest",
                    ApiKey = configuration["AiSettings:GeminiKey"]
                },
                new
                {
                    Name = "GitHubModels",
                    Url = "https://models.inference.ai.azure.com/chat/completions",
                    Model = "gpt-4o-mini",
                    ApiKey = configuration["AiSettings:GithubKey"]
                }
            ];
        }

        public async Task<Result<bool>> SubmitSupportAsync(SupportDto dto, string originDomain)
        {
            if (dto.Complaint != null && dto.Suggestion != null)
            {
                return Result<bool>.Failure("Only one of Complaint or Suggestion can be provided.", StatusCodes.Status400BadRequest);
            }

            if (string.IsNullOrEmpty(originDomain))
            {
                return Result<bool>.Failure("Origin domain is required for support requests.", StatusCodes.Status403Forbidden);
            }

            string subject = string.Empty;
            string body = string.Empty;
            FileDto? file = await _am.ConvertToFileDtoAsync(dto.Complaint?.Attachment);
            if (dto.Complaint != null) // Complaint submission
            {
                subject = $"Complaint from {dto.Name} ({dto.Email})";
                body = EmailTemplateBuilder.GenerateComplaintEmailBody(dto.Complaint, dto.Name, dto.Email, originDomain);
            }
            else if (dto.Suggestion != null) // Suggestion submission
            {
                subject = $"Suggestion from {dto.Name} ({dto.Email})";
                body = EmailTemplateBuilder.GenerateSuggestionEmailBody(dto.Suggestion, dto.Name, dto.Email, originDomain);
            }
            else {
                return Result<bool>.Failure("Either Complaint or Suggestion must be provided.", StatusCodes.Status400BadRequest);
            }

            _backgroundJobClient.Enqueue(() => _emailSenderService.NotifyAdminAsync(subject, body, dto.Email, CancellationToken.None, file));

            return Result<bool>.Success(true);
        }
      
        public async Task<Result<bool>> SubmitAsync(string publicApiKey, SubmitDto dto, string originDomain)
        {
            var project = await _uow.Projects.FindAsync(p => p.PublicApiKey == publicApiKey && p.IsActive && !p.IsDeleted);
            if (project == null)
            {
                return Result<bool>.Failure("Project not found or inactive.", StatusCodes.Status404NotFound);
            }

            if (project.IsLocked)
            {
                return Result<bool>.Failure("Project is locked.", StatusCodes.Status403Forbidden);
            }

            var user = await _uow.Users.FindAsync(u => u.Id == project.UserId && u.IsActive && !u.IsDeleted);
            if (user == null)
            {
                return Result<bool>.Failure("User not found or inactive.", StatusCodes.Status404NotFound);
            }

            bool hasPrivateKey = !string.IsNullOrEmpty(dto.PrivateKey);
            bool hasOrigin = !string.IsNullOrEmpty(originDomain);

            bool isBackendRequest = project.ProjectAccessMode == Project.AccessModes.BackendOnly.ToString() || (project.ProjectAccessMode == Project.AccessModes.Hybrid.ToString() && hasPrivateKey && !hasOrigin);
            if (isBackendRequest && (!hasPrivateKey || dto.PrivateKey != project.PrivateApiKey))
            {
               return Result<bool>.Failure("Invalid or missing Private API Key for backend request.", StatusCodes.Status403Forbidden);
            }

            if (!isBackendRequest && hasPrivateKey)
            {
                return Result<bool>.Failure("Private API Key should not be provided for frontend requests.", StatusCodes.Status403Forbidden);
            }


            List<string> allowedDomainsList = [];
            if (!string.IsNullOrEmpty(project.RestrictedDomains))
            {
                allowedDomainsList = [.. project.RestrictedDomains.Split(',').Select(d => d.Trim().ToLower())];
            }

            // التحقق من الدومين (CORS)
            if (!isBackendRequest)
            {
                if (!hasOrigin) // لو الطلب جاي من الفرونت اند ومش باعت الدومين تتحظر
                {
                    return Result<bool>.Failure("Origin domain is required for frontend requests.", StatusCodes.Status403Forbidden);
                }

                if (!string.IsNullOrEmpty(project.RestrictedDomains))
                {
                    string cleanOrigin = Uri.TryCreate(originDomain, UriKind.Absolute, out var uri) ? uri.Host : originDomain;
                    if (!allowedDomainsList.Contains(cleanOrigin.ToLower()))
                    {
                        return Result<bool>.Failure($"Origin domain '{cleanOrigin}' is not allowed for this project.", StatusCodes.Status403Forbidden);
                    }
                }
            }

            var template = await _uow.Templates.FindAsync(t => t.Id == dto.TemplateId && t.ProjectId == project.Id && t.IsActive && !t.IsDeleted);
            if (template == null) return Result<bool>.Failure("Template not found or inactive for this project.", StatusCodes.Status404NotFound);

            if (template.IsLocked)
            {
                return Result<bool>.Failure("Template is locked and cannot be used.", StatusCodes.Status403Forbidden);
            }

            // التحقق من ريكابتشا إذا كانت مفعلة في القالب
            if (!isBackendRequest)
            {
                if (template.EnableRecaptchaV2)
                {
                    if (string.IsNullOrEmpty(dto.RecaptchaToken))
                    {
                        return Result<bool>.Failure("ReCaptcha token is missing.", StatusCodes.Status403Forbidden);
                    }

                    var isValid = await VerifyRecaptchaAsync(user.Id, dto.RecaptchaToken, template.RecaptchaSecretKey, allowedDomainsList);
                    if (!isValid)
                    {
                        return Result<bool>.Failure("ReCaptcha verification failed.", StatusCodes.Status403Forbidden);
                    }
                }

                if (template.EnableAppCheck)
                {
                    if (string.IsNullOrEmpty(dto.AppCheckToken))
                    {
                        return Result<bool>.Failure("App Check token is missing.", StatusCodes.Status403Forbidden);
                    }

                    var isValidAppCheck = await VerifyAppCheckTokenAsync(user.Id, dto.AppCheckToken, template.AppCheckSecret, template.Issuer);
                    if (!isValidAppCheck)
                    {
                        return Result<bool>.Failure("App Check verification failed.", StatusCodes.Status403Forbidden);
                    }
                }
            }


            string finalRecipientEmail;
            string? finalRecipientName = null;
            if (isBackendRequest)
            {
                finalRecipientEmail = string.IsNullOrEmpty(dto.RecipientEmail) ? template.ToEmail : dto.RecipientEmail;
                finalRecipientName = string.IsNullOrEmpty(dto.RecipientName) || string.IsNullOrEmpty(dto.RecipientEmail) ? template.ToName : dto.RecipientName;
            }
            else
            {
                finalRecipientEmail = template.ToEmail;
            }

            var payloadJson = JsonSerializer.Serialize(dto.Fields);

            var submission = new Submission
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                TemplateId = template.Id,
                RecipientEmail = finalRecipientEmail,
                RecipientName = finalRecipientName,
                Subject = template.Subject,
                PayloadJson = template.DoSaveInHistory ? payloadJson : string.Empty,
                RawHtmlBody = template.DoSaveInHistory ? template.ContentHtml : string.Empty,
                ReceivedAt = DateTime.UtcNow,
                IsPrivateData = !template.DoSaveInHistory,
                Status = Submission.Statuses.Pending
            };

            await _uow.Submissions.AddAsync(submission);
            await _uow.CompleteAsync();

            _backgroundJobClient.Enqueue(() => ProcessInBackgroundAsync(submission.Id, user.Id, Submission.Statuses.Sent, finalRecipientName, finalRecipientEmail, dto.Fields));

            return Result<bool>.Success(true);
        }

        public async Task ProcessInBackgroundAsync(Guid submissionId, Guid userId, string status, string? recipientName, string recipientEmail, Dictionary<string, string> variables)
        {
            int rowsAffected = await _uow.UserRepository.DecreaseQuotaAsync(userId);

            var user = await _uow.Users.FindAsync(u => u.Id == userId);
            var submission = await _uow.Submissions.FindAsync(s => s.Id == submissionId);

            if (user == null || submission == null)
            {
                _logger.LogError(new Log(userId, $"User or Submission not found (SubmissionId: {submissionId}).", null, null));
                return;
            }

            if (rowsAffected == 0)
            {
                await ProcessQuotaExceededAsync(submission, user.Id);
                return;
            }

            var template = await _uow.Templates.FindAsync(t => t.Id == submission.TemplateId, includes: t => t.Include(t => t.TemplateAttachments));
            if (template == null)
            {
                _logger.LogError(new Log(userId, $"Template not found.", submission.ProjectId, null));
                await RefundQuotaAtomicallyAsync(userId);
                return;
            }

            var payloadJson = JsonSerializer.Serialize(variables);
            bool isAutoReplyEnabled = false;
            bool sendSuccess = false;

            // 💡 إضافة فكرتك: تتبع القوالب التي تم استخدامها لمنع التكرار الدائري
            var visitedTemplateIds = new HashSet<string>
            {
                template.Id.ToString() // أضف القالب الأول
            };

            do
            {
                isAutoReplyEnabled = template.EnableAutoReply;
                var result = GetFinalEmailContent(template.Subject, template.ContentHtml, recipientName, recipientEmail, template.ReplyTo, variables);

                if (!result.IsSuccess)
                {
                    await HandleSubmissionFailureAsync(submission, result.ErrorMessage, userId);
                    break;
                }

                List<string> cc = string.IsNullOrEmpty(template.Cc) ? [] : [.. template.Cc.Split(',').Select(c => c.Trim())];
                List<string> bcc = string.IsNullOrEmpty(template.Bcc) ? [] : [.. template.Bcc.Split(',').Select(b => b.Trim())];

                sendSuccess = await ProcessSendEmailAsync(submission, user, template.ServiceId, template.TemplateAttachments, status, result.Data.finalRecipientName, result.Data.finalSubject, result.Data.finalRecipientEmail, result.Data.finalHtmlBody, result.Data.finalReplyTo, cc, bcc);

                if (isAutoReplyEnabled && sendSuccess && !string.IsNullOrEmpty(template.AutoReplyTemplateId))
                {
                    var autoReplyId = template.AutoReplyTemplateId;

                    // 💡 تطبيق فكرتك: فحص ما إذا كان القالب قد تم استخدامه مسبقاً في هذه الدورة
                    if (visitedTemplateIds.Contains(autoReplyId))
                    {
                        _logger.LogWarning(new Log(userId, $"Circular auto-reply dependency detected! Template {autoReplyId} was already executed in this chain. Breaking loop to prevent infinite execution.", submission.ProjectId, null));

                        submission.Status = Submission.Statuses.Failed;
                        submission.ErrorMessage = "Auto-reply loop detected. The same template was triggered multiple times in a single submission chain.";
                        _uow.Submissions.Update(submission);
                        await _uow.CompleteAsync();

                        break; // الخروج من الحلقة فوراً
                    }

                    // إضافة القالب الجديد للسجل
                    visitedTemplateIds.Add(autoReplyId);

                    var nextTemplate = await _uow.Templates.FindAsync(t => t.Id == autoReplyId && t.ProjectId == submission.ProjectId && t.IsActive && !t.IsDeleted, includes: t => t.Include(t => t.TemplateAttachments));

                    if (nextTemplate == null || nextTemplate.IsLocked)
                    {
                        _logger.LogError(new Log(userId, $"Auto-reply template invalid or locked.", submission.ProjectId, null));
                        break;
                    }

                    submission = new Submission
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = nextTemplate.ProjectId,
                        TemplateId = nextTemplate.Id,
                        RecipientEmail = nextTemplate.ToEmail,
                        RecipientName = nextTemplate.ToName,
                        Subject = nextTemplate.Subject,
                        PayloadJson = nextTemplate.DoSaveInHistory ? payloadJson : string.Empty,
                        RawHtmlBody = nextTemplate.DoSaveInHistory ? nextTemplate.ContentHtml : string.Empty,
                        ReceivedAt = DateTime.UtcNow,
                        IsPrivateData = !nextTemplate.DoSaveInHistory,
                        Status = Submission.Statuses.Pending
                    };

                    await _uow.Submissions.AddAsync(submission);
                    await _uow.CompleteAsync();

                    recipientEmail = submission.RecipientEmail;
                    recipientName = submission.RecipientName;

                    int innerRowsAffected = await _uow.UserRepository.DecreaseQuotaAsync(userId);

                    if (innerRowsAffected == 0)
                    {
                        await ProcessQuotaExceededAsync(submission, userId);
                        break;
                    }

                    template = nextTemplate;
                    status = Submission.Statuses.Sent;
                }
                else
                {
                    break;
                }

            } while (isAutoReplyEnabled && sendSuccess);
        }

        private async Task<bool> ProcessSendEmailAsync(Submission submission, User user, string? serviceId, ICollection<TemplateAttachment> templateAttachments, string status, string? finalRecipientName, string finalSubject, string recipientEmail, string finalHtmlBody, string? replyTo, List<string> cc, List<string> bcc)
        {
            var sendDto = new SendEmailDto
            {
                ProjectId = submission.ProjectId,
                ServiceId = serviceId,
                ToEmail = recipientEmail,
                ToName = finalRecipientName ?? "",
                Subject = finalSubject,
                Body = finalHtmlBody,
                ReplyTo = replyTo,
                Cc = cc,
                Bcc = bcc,
                Attachments = [.. templateAttachments.Select(a => _am.ConvertToIFormFile(a.FileUrl))]
            };

            try
            {
                var responseDto = await _emailSenderService.SendEmailAsync(sendDto);
                if (responseDto.Success)
                {
                    await ApplyAllIntegrations(submission, user.Id, submission.ProjectId, sendDto, responseDto);
                    submission.Status = status;
                    submission.SentAt = DateTime.UtcNow;
                }
                else
                {
                    await RefundQuotaAtomicallyAsync(user.Id);
                    submission.Status = Submission.Statuses.Failed;
                    submission.ErrorMessage = responseDto.ErrorMessage ?? "Unknown error during email sending.";
                }

                _uow.Submissions.Update(submission);
                await _uow.CompleteAsync();
                return responseDto.Success;
            }
            catch (Exception ex)
            {
                await RefundQuotaAtomicallyAsync(user.Id);
                submission.Status = Submission.Statuses.Failed;
                submission.ErrorMessage = $"Error during email sending: {ex.Message}";
                _uow.Submissions.Update(submission);
                await _uow.CompleteAsync();

                _logger.LogCritical(new Log(user.Id, $"Critical error in background job: {ex.Message}", submission.ProjectId, ex.ToString()));
                return false;
            }
        }

        private async Task HandleSubmissionFailureAsync(Submission submission, string? error, Guid userId)
        {
            await RefundQuotaAtomicallyAsync(userId);
            submission.Status = Submission.Statuses.Failed;
            submission.ErrorMessage = error;
            _uow.Submissions.Update(submission);
            await _uow.CompleteAsync();
        }

        private async Task RefundQuotaAtomicallyAsync(Guid userId)
        {
            // إرجاع الحصة بطريقة ذرية لمنع أي Race condition
            await _uow.UserRepository.IncreaseQuotaAsync(userId);
        }

        private async Task ProcessQuotaExceededAsync(Submission submission, Guid userId)
        {
            var subscription = await _uow.Subscriptions.FindAsync(s => s.UserId == userId && s.Status == Subscription.Statuses.Active);
            var plan = subscription != null ? await _uow.Plans.FindAsync(p => p.Id == subscription.PlanId && p.IsActive) : null;

            if (subscription == null || plan == null)
            {
                _logger.LogError(new Log(userId, "No active subscription/plan found."));
                return;
            }

            // زيادة ذرية لعداد التجاوز
            await _uow.UserRepository.IncreaseOverageEmailsAsync(userId);
               
            if (plan.Name != "Free")
            {
                submission.Status = Submission.Statuses.QuotaExceeded;
                submission.ErrorMessage = "Quota exceeded for your plan. Submission blocked.";
                _uow.Submissions.Update(submission);
            }
            else {
                // عدم حفظ أي بيانات للطلبات التي تجاوزت الحصة في الخطة المجانية
                _uow.Submissions.Delete(submission);
            }
            await _uow.CompleteAsync();
        }
      
        private async Task<bool> VerifyRecaptchaAsync(Guid userId, string token, string? secretKey, List<string> allowedDomainsList)
        {
            if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(token))
                return false;

            var content = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("secret", secretKey),
                new KeyValuePair<string, string>("response", token)
            ]);

            try
            {
                using var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);

                if (!response.IsSuccessStatusCode)
                    return false;

                var jsonResult = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(jsonResult);
                var root = document.RootElement;

                if (!root.TryGetProperty("success", out var success) || !success.GetBoolean())
                    return false;

                if (root.TryGetProperty("hostname", out var hostname))
                {
                    string? actualHost = hostname.GetString()?.ToLower();

                    // تنظيف النطاقات المسموحة من http:// و https:// لضمان التطابق
                    List<string> cleanAllowedDomains = [.. allowedDomainsList.Select(d => d.Replace("http://", "").Replace("https://", "").Split(':')[0].ToLower())];

                    if (actualHost == null || 
                        ( cleanAllowedDomains.Count > 0
                        && !cleanAllowedDomains.Contains(actualHost)))
                    {
                        return false;
                    }
                }

                if (root.TryGetProperty("score", out var scoreElement))
                {
                    var score = scoreElement.GetDouble();
                    if (score < 0.5)
                        return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(new Log(userId, $"Exception during ReCaptcha: {ex.Message}", null, null));
                return false;
            }
        }

        private async Task<bool> VerifyAppCheckTokenAsync(Guid userId, string token, string? projectAppCheckSecret, string? expectedIssuer = null)
        {
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                if (!tokenHandler.CanReadToken(token))
                {
                    return false;
                }
                var jwtToken = tokenHandler.ReadJwtToken(token);

                var algorithm = jwtToken.Header.Alg;
                var tokenIssuer = jwtToken.Issuer;

                var validationParameters = new TokenValidationParameters
                {
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidateAudience = false
                };

                // 1. التعامل مع التشفير المتماثل (Custom App)
                if (algorithm == SecurityAlgorithms.HmacSha256)
                {
                    if (string.IsNullOrEmpty(projectAppCheckSecret))
                    {
                        return false;
                    }

                    validationParameters.ValidateIssuerSigningKey = true;
                    validationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(projectAppCheckSecret));

                    validationParameters.ValidateIssuer = !string.IsNullOrEmpty(expectedIssuer);
                    if (validationParameters.ValidateIssuer)
                    {
                        validationParameters.ValidIssuer = expectedIssuer;
                    }

                    var principal = await tokenHandler.ValidateTokenAsync(token, validationParameters);
                    return principal.IsValid;
                }
                // 2. التعامل مع المزودين الخارجيين (RS256)

                else if (algorithm == SecurityAlgorithms.RsaSha256)
                {

                    if (string.IsNullOrEmpty(tokenIssuer))
                    {
                        return false;
                    }

                    // توجيه ذكي لـ Firebase
                    if (tokenIssuer.Contains("firebaseappcheck.googleapis.com"))
                    {
                        var jwksUri = "https://firebaseappcheck.googleapis.com/v1/jwks";

                        using var httpClient = new HttpClient();
                        var jsonJwks = await httpClient.GetStringAsync(jwksUri);

                        // جلب المفاتيح العامة مباشرة
                        var jwks = new JsonWebKeySet(jsonJwks);

                        validationParameters.ValidateIssuerSigningKey = true;
                        validationParameters.IssuerSigningKeys = jwks.GetSigningKeys();
                        validationParameters.ValidateIssuer = true;
                        validationParameters.ValidIssuer = tokenIssuer; // أو رابط فايربيز الثابت حسب رغبتك

                        var principal = await tokenHandler.ValidateTokenAsync(token, validationParameters);
                        return principal.IsValid;
                    }
                    else
                    {
                        // باقي المزودين القياسيين (Auth0, AWS, etc.)
                        var discoveryEndpoint = $"{tokenIssuer.TrimEnd('/')}/.well-known/openid-configuration";

                        var configurationManager = new Microsoft.IdentityModel.Protocols.ConfigurationManager<OpenIdConnectConfiguration>(
                            discoveryEndpoint,
                            new OpenIdConnectConfigurationRetriever(),
                            new HttpDocumentRetriever { RequireHttps = true });

                        var openIdConfig = await configurationManager.GetConfigurationAsync(CancellationToken.None);

                        validationParameters.ValidateIssuerSigningKey = true;
                        validationParameters.IssuerSigningKeys = openIdConfig.SigningKeys;
                        validationParameters.ValidateIssuer = true;
                        validationParameters.ValidIssuer = tokenIssuer;

                        var principal = await tokenHandler.ValidateTokenAsync(token, validationParameters);
                        return principal.IsValid;
                    }
                }
                else
                {
                    _logger.LogWarning(new Log(userId, $"Unsupported JWT Algorithm: {algorithm}", null, null));
                    return false;
                }
            }
            catch (SecurityTokenException)
            {
                _logger.LogWarning(new Log(userId, "Invalid or Expired App Check token.", null, null));
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(new Log(userId, $"Exception during App Check verification: {ex.Message}", null, null));
                return false;
            }
        }

        private static Result<(string finalSubject, string finalHtmlBody, string? finalRecipientName, string finalRecipientEmail, string? finalReplyTo)>
     GetFinalEmailContent(string subject, string contentHtml, string? recipientName, string recipientEmail, string? replyTo, Dictionary<string, string> variables)
        {
            string finalSubject = subject;
            string finalHtmlBody = contentHtml;
            string? finalRecipientName = recipientName;
            string finalRecipientEmail = recipientEmail;
            string? finalReplyTo = replyTo;

            if (variables != null && variables.Count > 0)
            {
                foreach (var variable in variables)
                {
                    string placeholder = "{{" + variable.Key + "}}";

                    finalRecipientEmail = finalRecipientEmail.Replace(placeholder, variable.Value);
                    finalRecipientName = finalRecipientName?.Replace(placeholder, variable.Value);
                    finalReplyTo = finalReplyTo?.Replace(placeholder, variable.Value);
                    finalSubject = finalSubject.Replace(placeholder, variable.Value);
                    finalHtmlBody = finalHtmlBody.Replace(placeholder, variable.Value);
                }
            }

            var validEmailAttr = new ValidEmailAttribute(AllowNull: false);
            if (!validEmailAttr.IsValid(finalRecipientEmail))
                return Result<(string, string, string?, string, string?)>.Failure("Final recipient email is invalid after variable replacement.", StatusCodes.Status400BadRequest);

            var validReplyToAttr = new ValidEmailAttribute(AllowNull: true);
            if (!validReplyToAttr.IsValid(finalReplyTo))
                return Result<(string, string, string?, string, string?)>.Failure("Final replyTo email is invalid after variable replacement.", StatusCodes.Status400BadRequest);

            if (string.IsNullOrEmpty(finalHtmlBody))
                return Result<(string, string, string?, string, string?)>.Failure("Final HTML body is empty after variable replacement.", StatusCodes.Status400BadRequest);

            if (!HtmlSecurityValidator.IsHtmlSafe(finalHtmlBody))
                return Result<(string, string, string?, string, string?)>.Failure("Final HTML body contains unsafe content after sanitization.", StatusCodes.Status403Forbidden);

            return Result<(string FinalSubject, string FinalHtmlBody, string? FinalRecipientName, string FinalRecipientEmail, string? FinalReplyTo)>.Success(
                (finalSubject, finalHtmlBody, finalRecipientName, finalRecipientEmail, finalReplyTo)
            );
        }
        private async Task ApplyAllIntegrations(Submission submission, Guid UserId, string ProjectId, SendEmailDto sendDto, SendEmailResponseDto ResponseDto)
        {
            var subscription = await _uow.Subscriptions.FindAsync(s => s.UserId == UserId && s.Status == Subscription.Statuses.Active,
                includes: s => s.Include(s => s.Plan));

            if (subscription == null || subscription.Plan == null) return;

            var plan = subscription.Plan;
            bool isIntegrationAllowed = plan.CanUseGoogleSheets || plan.CanUseAi || plan.CanUseTelegramBot;
            if (!isIntegrationAllowed) return;

            var integrations = await _uow.Integrations.FindAllAsync(s => s.ProjectId == ProjectId && s.IsActive && !s.IsDeleted, includes: s => s.Include(s => s.Project));

            if (integrations == null) return;

            foreach (var integration in integrations)
            {
                try
                {
                    if (integration.IntegrationType == Integration.IntegrationTypes.TelegramBot.ToString() && plan.CanUseTelegramBot)
                    {
                        await ExecuteTelegramBotIntegrationAsync(UserId, integration, sendDto, ResponseDto);
                    }
                    else if (integration.IntegrationType == Integration.IntegrationTypes.GoogleSheets.ToString() && plan.CanUseGoogleSheets)
                    {
                        await ExecuteGoogleSheetsIntegrationAsync(UserId, integration, sendDto, ResponseDto);
                    }
                    else if (integration.IntegrationType == Integration.IntegrationTypes.AiSummary.ToString() && plan.CanUseAi)
                    {
                        if (submission.RecipientEmail != string.Empty) // فقط لو تم حفظ البريد في التاريخ (History)
                            await ExecuteAiIntegrationAsync(submission, sendDto, UserId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(new Log(UserId, $"Error executing integration {integration.Id} of type {integration.IntegrationType}: {ex.Message}", ProjectId, ex.ToString()));
                }
            }
        }

        private async Task<string?> GenerateSummaryAsync(string subject, string body, Guid userId)
        {
            string systemPrompt = "You are a highly efficient assistant. Summarize the following email concisely in bullet points. Focus on the main intent and any required actions.";
            string userPrompt = $"Subject: {subject}\nBody: {body}";

            return await CallAiProvidersAsync(systemPrompt, userPrompt, "Summary", userId);

        }

        private async Task<string?> GenerateSuggestedReplyAsync(string subject, string body, Guid userId)
        {
            string systemPrompt = "You are a professional customer support assistant. Draft a polite, clear, and helpful reply to the following email. Keep it concise.";
            string userPrompt = $"Subject: {subject}\nBody: {body}";

            return await CallAiProvidersAsync(systemPrompt, userPrompt, "Suggested Reply", userId);
        }

        private async Task<string?> CallAiProvidersAsync(string systemPrompt, string userPrompt, string taskName, Guid userId)
        {
            var errorLogs = new List<string>();

            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            foreach (var provider in _aiProviders)
            {
                if (string.IsNullOrEmpty(provider.ApiKey))
                {
                    errorLogs.Add($"[{provider.Name}] API Key is missing.");
                    continue;
                }

                try
                {
                    var requestBody = new
                    {
                        model = provider.Model,
                        messages = new[]
                        {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                        temperature = 0.7,
                        max_tokens = 500
                    };

                    using var request = new HttpRequestMessage(HttpMethod.Post, provider.Url);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", provider.ApiKey);

                    // هيدر User-Agent إجباري لخوادم GitHub/Azure وإلا سيتم رفض الطلب
                    request.Headers.TryAddWithoutValidation("User-Agent", "EmailyApp/1.0");
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    request.Content = JsonContent.Create(requestBody);

                    var response = await client.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseData = await response.Content.ReadFromJsonAsync<JsonElement>();

                        var choices = responseData.GetProperty("choices");
                        var message = choices[0].GetProperty("message");

                        if (message.TryGetProperty("content", out var contentProp))
                        {
                            string? aiResponseText = contentProp.GetString()?.Trim();

                            if (!string.IsNullOrEmpty(aiResponseText))
                            {
                                return aiResponseText;
                            }
                            else
                            {
                                errorLogs.Add($"[{provider.Name}] Success, but returned empty content.");
                            }
                        }
                    }
                    else
                    {
                        string errorDetails = await response.Content.ReadAsStringAsync();
                        errorLogs.Add($"[{provider.Name}] Failed with status {response.StatusCode}: {errorDetails}");
                    }
                }
                catch (TaskCanceledException)
                {
                    errorLogs.Add($"[{provider.Name}] Request timed out.");
                }
                catch (Exception ex)
                {
                    errorLogs.Add($"[{provider.Name}] Exception: {ex.Message}");
                }
            }

            string combinedErrors = string.Join(" | ", errorLogs);
            _logger.LogError(new Log(userId, $"All AI providers failed for task '{taskName}': {combinedErrors}"));

            return null;
        }
      
        private async Task ExecuteAiIntegrationAsync(Submission submission, SendEmailDto sendDto, Guid userId)
        {

            string? summary = await GenerateSummaryAsync(sendDto.Subject, sendDto.Body, userId);
            if (!string.IsNullOrEmpty(summary))
            {
                submission.AiSummary = summary;
                // سيتم التحديث بعد الرجوع من جميع التكاملات، لذلك لا حاجة للتحديث هنا مباشرة
            }
        }

        private async Task ExecuteTelegramBotIntegrationAsync(Guid userId, Integration integration, SendEmailDto sendDto, SendEmailResponseDto responseDto)
        {
            try
            {
                using var jsonDocument = JsonDocument.Parse(integration.ConfigJson);
                if (!jsonDocument.RootElement.TryGetProperty("chatId", out var chatIdElement))
                {
                    _logger.LogWarning(new Log(userId, $"'ChatId' key is missing in ConfigJson for integration {integration.Id}."));
                    return;
                }
                string chatId = chatIdElement.GetString()!;

                // 3. بناء نص الرسالة التنبيهية بتنسيق HTML لتكون منظمة وسهلة القراءة
                string statusIcon = responseDto.Success ? "✅" : "❌";
                string statusText = responseDto.Success ? "نجاح" : "فشل";

                string messageText = $@"
<b>🔔 تحديث إرسال بريد (Emaily)</b>
<b>الحالة:</b> {statusIcon} {statusText}
<b>المشروع:</b> <code>{sendDto.ProjectId}</code>
<b>إلى:</b> {sendDto.ToEmail}
<b>الموضوع:</b> {sendDto.Subject ?? "<i>بدون موضوع</i>"}
";

                // إضافة سبب الفشل إن وجد
                if (!responseDto.Success && !string.IsNullOrEmpty(responseDto.ErrorMessage))
                {
                    messageText += $"\n<b>الخطأ:</b> <code>{responseDto.ErrorMessage}</code>";
                }

                // 4. تجهيز الطلب وإرساله إلى Telegram API
                var payload = new
                {
                    chat_id = chatId,
                    text = messageText,
                    parse_mode = "HTML" // لتفعيل تنسيقات البولد والأكواد
                };

                string telegramUrl = $"https://api.telegram.org/bot{_botToken}/sendMessage";

                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(10); // تحديد وقت أقصى لتجنب تعليق الـ Thread

                var response = await client.PostAsJsonAsync(telegramUrl, payload);

                if (!response.IsSuccessStatusCode)
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning(new Log(userId, $"Telegram integration failed for {integration.Id}. Status: {response.StatusCode}. Details: {errorResponse}"));
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(new Log(userId, $"Failed to parse ConfigJson for Telegram integration {integration.Id}: {ex.Message}"));
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning(new Log(userId, $"Telegram API request timed out for integration {integration.Id}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(new Log(userId, $"Unexpected error executing Telegram integration {integration.Id}: {ex.Message}"));
            }
        }

        private async Task ExecuteGoogleSheetsIntegrationAsync(Guid userId, Integration integration, SendEmailDto sendDto, SendEmailResponseDto responseDto)
        {

            try
            {

                using var jsonDocument = JsonDocument.Parse(integration.ConfigJson);
                if (!jsonDocument.RootElement.TryGetProperty("sheetUrl", out var urlElement))
                {
                    _logger.LogWarning(new Log(userId, $"'SheetUrl' key is missing in ConfigJson for integration {integration.Id}.", sendDto.ProjectId));
                    return;
                }

                string sheetUrl = urlElement.GetString()!;

                // 2. استخراج الـ Spreadsheet ID من الرابط
                var match = MyRegex().Match(sheetUrl);
                if (!match.Success)
                {
                    _logger.LogWarning(new Log(userId, $"Invalid Google Sheet URL format for integration {integration.Id}.", sendDto.ProjectId));
                    return;
                }
                string spreadsheetId = match.Groups[1].Value;

                GoogleCredential credential;
                try
                {
                    #pragma warning disable CS0618 // نخبر الـ Compiler أننا نثق في مصدر البيانات وأنه آمن (من متغيرات البيئة)

                    credential = GoogleCredential.FromJson(_jsonCredentials).CreateScoped(SheetsService.Scope.Spreadsheets);
                  
                    #pragma warning restore CS0618 // نعيد تفعيل التحذير لباقي أجزاء الكود لحمايته
                }
                catch (Exception ex)
                {
                    _logger.LogError(new Log(userId, "Failed to parse Google Sheets credentials from environment variables.", sendDto.ProjectId, ex.ToString()));
                    return;
                }

                // 4. إنشاء خدمة الاتصال
                var service = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Emaily App"
                });

                // 5. تجهيز البيانات
                var valueRange = new ValueRange
                {
                    Values =
                    [
                        [
                            DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                            sendDto.ProjectId,
                            sendDto.ToEmail,
                            sendDto.Subject ?? "بدون موضوع",
                            responseDto.Success ? "نجاح" : "فشل",
                            responseDto.ErrorMessage ?? ""
                        ]
                    ]
                };

                // 6. إرسال أمر الإضافة
                // أولاً: جلب بيانات الملف لمعرفة اسم أول شيت ديناميكياً
                var getMetadataRequest = service.Spreadsheets.Get(spreadsheetId);
                var spreadsheetMetadata = await getMetadataRequest.ExecuteAsync();
                string actualSheetName = spreadsheetMetadata.Sheets.FirstOrDefault()?.Properties?.Title ?? "Sheet1";

                // ثانياً: إرسال البيانات باستخدام الاسم الحقيقي
                var appendRequest = service.Spreadsheets.Values.Append(valueRange, spreadsheetId, actualSheetName);
                appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;

                var appendResponse = await appendRequest.ExecuteAsync();

            }
            catch (Google.GoogleApiException gEx)
            {
                _logger.LogError(new Log(userId, $"Google API Error: {gEx.Error?.Message ?? gEx.Message} - HTTP {gEx.HttpStatusCode}", sendDto.ProjectId, gEx.ToString()));
            }
            catch (Exception ex)
            {
                _logger.LogError(new Log(userId, $"Unexpected error executing Google Sheets integration {integration.Id}", sendDto.ProjectId, ex.ToString()));
            }
        }

        public async Task<Result<PagedResultDto<SubmissionHistoryDto>>> GetProjectSubmissionsAsync(Guid userId, string projectId, int page)
        {
            // التحقق من ملكية المشروع
            var project = await _uow.Projects.FindAsync(p => p.Id == projectId && p.UserId == userId && !p.IsDeleted);
            if (project == null) return Result<PagedResultDto<SubmissionHistoryDto>>.Failure("Project not found or access denied.", StatusCodes.Status404NotFound);

            // جلب البيانات بالترقيم
            var (Items, TotalCount) = await _uow.Submissions.GetPagedAsync(s => s.ProjectId == projectId, page, _pageSize, orderBy: x => x.ReceivedAt, isDescending: true);

            return Result<PagedResultDto<SubmissionHistoryDto>>.Success(
            new PagedResultDto<SubmissionHistoryDto>
            {
                // [.. select-query ] = select-query.ToList()
                Items = [.. Items.Select(s => new SubmissionHistoryDto
                {
                    Id = s.Id.ToString(),
                    RecipientEmail = s.RecipientEmail,
                    Subject = s.Subject,
                    Status = s.Status,
                    ReceivedAt = s.ReceivedAt,
                    SentAt = s.SentAt
                })],
                TotalCount = TotalCount,
                CurrentPage = page,
                PageSize = _pageSize
            });
        }

        public async Task<Result<SubmissionDetailsDto>> GetSubmissionDetailsAsync(Guid userId, string submissionId)
        {
            if (!Guid.TryParse(submissionId, out var subGuid)) return Result<SubmissionDetailsDto>.Failure("Invalid Submission ID format.", StatusCodes.Status400BadRequest);

            var submission = await _uow.Submissions.FindAsync(s => s.Id == subGuid);
            if (submission == null) return Result<SubmissionDetailsDto>.Failure("Submission not found.", StatusCodes.Status404NotFound);

            var project = await _uow.Projects.FindAsync(p => p.Id == submission.ProjectId && p.UserId == userId && !p.IsDeleted);
            if (project == null) return Result<SubmissionDetailsDto>.Failure("Access denied.", StatusCodes.Status403Forbidden);

            return Result<SubmissionDetailsDto>.Success(
            new SubmissionDetailsDto
            {
                Id = submission.Id.ToString(),
                TemplateId = submission.TemplateId,
                RecipientEmail = submission.RecipientEmail,
                Subject = submission.Subject,
                Status = submission.Status,
                ReceivedAt = submission.ReceivedAt,
                SentAt = submission.SentAt,
                PayloadJson = submission.PayloadJson,
                RawHtmlBody = submission.RawHtmlBody,
                ErrorMessage = submission.ErrorMessage,
                AiSummary = submission.AiSummary
            });
        }

        public async Task<Result<string>> GetSubmissionStatusAsync(Guid userId, string submissionId)
        {
            if (!Guid.TryParse(submissionId, out var subGuid)) return Result<string>.Failure("Invalid Submission ID format.", StatusCodes.Status400BadRequest);
            var submission = await _uow.Submissions.FindAsync(s => s.Id == subGuid);
            if (submission == null) return Result<string>.Failure("Submission not found.", StatusCodes.Status404NotFound);
            var project = await _uow.Projects.FindAsync(p => p.Id == submission.ProjectId && p.UserId == userId && !p.IsDeleted);
            if (project == null) return Result<string>.Failure("Access denied.", StatusCodes.Status403Forbidden);
            return Result<string>.Success(submission.Status);
        }
       
        public async Task<Result<SubmissionHistoryDto>> RetrySubmissionAsync(Guid userId, string submissionId)
        {
            if (!Guid.TryParse(submissionId, out var subGuid)) return Result<SubmissionHistoryDto>.Failure("Invalid Submission ID format.", StatusCodes.Status400BadRequest);

            var user = await _uow.Users.FindAsync(u => u.Id == userId && !u.IsDeleted && u.IsActive);
            if (user == null) return Result<SubmissionHistoryDto>.Failure("User not found or inactive.", StatusCodes.Status404NotFound);

            var submission = await _uow.Submissions.FindAsync(s => s.Id == subGuid);
            if (submission == null) return Result<SubmissionHistoryDto>.Failure("Submission not found.", StatusCodes.Status404NotFound);

            var project = await _uow.Projects.FindAsync(p => p.Id == submission.ProjectId && p.UserId == userId && !p.IsDeleted);
            if (project == null) return Result<SubmissionHistoryDto>.Failure("Access denied.", StatusCodes.Status403Forbidden);

            if (project.IsLocked)
                return Result<SubmissionHistoryDto>.Failure("Cannot retry submission: The project is locked due to plan limits.", StatusCodes.Status403Forbidden);

            var IsTemplateLocked = await _uow.Templates.SelectWhereAsync(selector: t => t.IsLocked, criteria: t => t.Id == submission.TemplateId);
            if (IsTemplateLocked.FirstOrDefault() == true)
                return Result<SubmissionHistoryDto>.Failure("Cannot retry submission: The template is locked due to plan limits.", StatusCodes.Status403Forbidden);

            // التحقق من حالة الفشل بناءً على متطلبات الـ PDF
            if (submission.Status == Submission.Statuses.Sent || submission.Status == Submission.Statuses.Resent)
                return Result<SubmissionHistoryDto>.Failure("Cannot retry a submission that has already been sent successfully.", StatusCodes.Status400BadRequest);

            if (submission.Status == Submission.Statuses.Pending)
                return Result<SubmissionHistoryDto>.Failure("Cannot retry a submission that is still pending.", StatusCodes.Status400BadRequest);

            if (submission.IsPrivateData)
                return Result<SubmissionHistoryDto>.Failure("Cannot retry: This action was not saved to the history based on the template's settings when the email was sent.", StatusCodes.Status400BadRequest);
            
            // استخراج المتغيرات المحفوظة من الطلب الأصلي
            var variables = string.IsNullOrEmpty(submission.PayloadJson)
                ? []
                : JsonSerializer.Deserialize<Dictionary<string, string>>(submission.PayloadJson) ?? [];

            submission.Status = Submission.Statuses.Pending; // تحديث الحالة إلى Pending قبل إعادة الإرسال
            _uow.Submissions.Update(submission);
            await _uow.CompleteAsync();

            _backgroundJobClient.Enqueue(() => ProcessInBackgroundAsync(subGuid, userId, Submission.Statuses.Resent, submission.RecipientName, submission.RecipientEmail, variables));

            var submissionDto = new SubmissionHistoryDto
            {
                Id = submission.Id.ToString(),
                RecipientEmail = submission.RecipientEmail,
                Subject = submission.Subject,
                Status = submission.Status,
                ReceivedAt = submission.ReceivedAt,
                SentAt = submission.SentAt
            };

            return Result<SubmissionHistoryDto>.Success(submissionDto);
        }

        [GeneratedRegex(@"/d/([a-zA-Z0-9-_]+)")]
        private static partial Regex MyRegex();
    }
}