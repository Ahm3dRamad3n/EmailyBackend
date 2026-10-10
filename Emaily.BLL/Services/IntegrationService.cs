using Emaily.BLL.DTOs;
using Emaily.BLL.DTOs.Integration;
using Emaily.BLL.Helpers.Interfaces;
using Emaily.BLL.Interfaces;
using Emaily.DAL.Entities;
using Emaily.DAL.Interfaces;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Emaily.BLL.Services
{
    public partial class IntegrationService(IUnitOfWork uow, IHttpClientFactory httpClientFactory, ILoggerService logger, IConfiguration configuration) : IIntegrationService
    {
        private readonly IUnitOfWork _uow = uow;
        private readonly ILoggerService _logger = logger;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly string _botToken = configuration["TELEGRAM_BOT_TOKEN"] ??
                    throw new Exception("Telegram Bot Token is not set in environment variables.");
        private readonly string _jsonCredentials = configuration["GOOGLE_SHEETS_CREDENTIALS"] ??
                     throw new Exception("Google Sheets credentials are not set in environment variables.");

        public async Task<Result<IEnumerable<IntegrationDto>>> GetIntegrationsAsync(string projectId)
        {
            var integrations = await _uow.Integrations.FindAllAsync(i => i.ProjectId == projectId && !i.IsDeleted);
            return Result<IEnumerable<IntegrationDto>>.Success(integrations.Select(i => new IntegrationDto
            {
                Id = i.Id.ToString(),
                ProjectId = i.ProjectId,
                IntegrationType = i.IntegrationType,
                ConfigJson = i.ConfigJson,
                IsActive = i.IsActive
            }));
        }

        public async Task<Result<IntegrationDto>> AddIntegrationAsync(Guid userId, string projectId, CreateIntegrationDto dto)
        {
            var subscriptions = await _uow.Subscriptions.FindAllAsync(s => s.UserId == userId && s.Status == Subscription.Statuses.Active && s.EndDate > DateTime.UtcNow);
            var activeSubscription = subscriptions.OrderByDescending(s => s.CreatedAt).FirstOrDefault();
            if (activeSubscription == null)
                return Result<IntegrationDto>.Failure("No active subscription found. Please subscribe to a plan to add integrations.", StatusCodes.Status403Forbidden);


            var plan = await _uow.Plans.FindAsync(p => p.Id == activeSubscription.PlanId);
            if (plan == null)
                return Result<IntegrationDto>.Failure("Subscription plan not found.", StatusCodes.Status403Forbidden);

            var integrationType = dto.IntegrationType;

            if (integrationType == Integration.IntegrationTypes.TelegramBot && !plan.CanUseTelegramBot)
                return Result<IntegrationDto>.Failure("Your current plan does not support Telegram Bot integration", StatusCodes.Status403Forbidden);

            if (integrationType == Integration.IntegrationTypes.GoogleSheets && !plan.CanUseGoogleSheets)
                return Result<IntegrationDto>.Failure("Your current plan does not support Google Sheets integration", StatusCodes.Status403Forbidden);

            if (integrationType == Integration.IntegrationTypes.AiSummary && !plan.CanUseAi)
                return Result<IntegrationDto>.Failure("Your current plan does not support AI Summary integration", StatusCodes.Status403Forbidden);

            var configString = dto.ConfigJson.GetRawText();

            if (integrationType == Integration.IntegrationTypes.TelegramBot)
            {
                var isValid = await VerifyTelegramConfigAsync(userId, configString);
                if (!isValid) return Result<IntegrationDto>.Failure("Invalid Telegram Bot Token or Chat ID. Could not send test message.", StatusCodes.Status400BadRequest);
                integrationType = Integration.IntegrationTypes.TelegramBot;
            }
            else if (integrationType == Integration.IntegrationTypes.GoogleSheets)
            {
                var isValid = await VerifyGoogleSheetsConfigAsync(userId, configString);
                if (!isValid) return Result<IntegrationDto>.Failure("Invalid Google Sheets URL or permission denied. Make sure you shared the sheet with our Service Account email.", StatusCodes.Status400BadRequest);
                integrationType = Integration.IntegrationTypes.GoogleSheets;
            }
            else
            {
                configString = "Integration type does not require specific configuration."; 
                integrationType = Integration.IntegrationTypes.AiSummary;
            }

            // منع تكرار نفس نوع التكامل لنفس المشروع
            var existingIntegrations = await _uow.Integrations.FindAllAsync(i => i.ProjectId == projectId && !i.IsDeleted);
            if (existingIntegrations.Any(i => i.IntegrationType == integrationType))
               return Result<IntegrationDto>.Failure($"An integration of type {integrationType} already exists for this project.", StatusCodes.Status400BadRequest);

            // 3. الحفظ في قاعدة البيانات
            var integration = new Integration
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                IntegrationType = integrationType,
                ConfigJson = configString,
                IsActive = true,
                IsDeleted = false
            };

            await _uow.Integrations.AddAsync(integration);
            await _uow.CompleteAsync();

            return Result<IntegrationDto>.Success(new IntegrationDto
            {
                Id = integration.Id.ToString(),
                ProjectId = integration.ProjectId,
                IntegrationType = integration.IntegrationType,
                ConfigJson = integration.ConfigJson,
                IsActive = integration.IsActive
            });
        }

        public async Task<Result<IntegrationDto>> UpdateIntegrationAsync(Guid userId, string integrationId, UpdateIntegrationDto dto)
        {
            if (!Guid.TryParse(integrationId, out var parsedId)) return Result<IntegrationDto>.Failure("Invalid integration ID format.", StatusCodes.Status400BadRequest);
            var integration = await _uow.Integrations.FindAsync(i => i.Id == parsedId);
            if (integration == null) return Result<IntegrationDto>.Failure("Integration not found.", StatusCodes.Status404NotFound);

            var configString = dto.ConfigJson.GetRawText();
            if (integration.IntegrationType == Integration.IntegrationTypes.TelegramBot)
            {
                var isValid = await VerifyTelegramConfigAsync(userId, configString);
                if (!isValid) return Result<IntegrationDto>.Failure("Invalid Telegram Bot Token or Chat ID. Could not send test message.", StatusCodes.Status400BadRequest);
            }
            else if (integration.IntegrationType == Integration.IntegrationTypes.GoogleSheets)
            {
                var isValid = await VerifyGoogleSheetsConfigAsync(userId, configString);
                if (!isValid) return Result<IntegrationDto>.Failure("Invalid Google Sheets URL or permission denied. Make sure you shared the sheet with our Service Account email.", StatusCodes.Status400BadRequest);
            }
            else
            {
                configString = integration.ConfigJson; // لا تقم بتغيير الإعدادات إذا كان التكامل من نوع آخر
            }

            integration.ConfigJson = configString;
            _uow.Integrations.Update(integration);
            await _uow.CompleteAsync();

            return Result<IntegrationDto>.Success(new IntegrationDto
            {
                Id = integration.Id.ToString(),
                ProjectId = integration.ProjectId,
                IntegrationType = integration.IntegrationType,
                ConfigJson = integration.ConfigJson,
                IsActive = integration.IsActive
            });
        }

        public async Task<bool> RemoveIntegrationAsync(string integrationId)
        {
            if (!Guid.TryParse(integrationId, out var parsedId)) return false;

            var integration = await _uow.Integrations.FindAsync(i => i.Id == parsedId && !i.IsDeleted);
            if (integration == null) return false;

            integration.IsDeleted = true;
            integration.IsActive = false;

            _uow.Integrations.Update(integration);
            await _uow.CompleteAsync();
            return true;
        }

        public async Task<bool> ToggleStatusAsync(string integrationId, bool isActive)
        {
            if (!Guid.TryParse(integrationId, out var parsedId)) return false;

            var integration = await _uow.Integrations.FindAsync(i => i.Id == parsedId && !i.IsDeleted);
            if (integration == null) return false;

            integration.IsActive = isActive;
            _uow.Integrations.Update(integration);
            await _uow.CompleteAsync();
            return true;
        }

        private async Task<bool> VerifyTelegramConfigAsync(Guid userId, string configJson)
        {
            try
            {
                using var document = JsonDocument.Parse(configJson);
                if (!document.RootElement.TryGetProperty("chatId", out var chatIdProp))
                {
                    return false;
                }

                var chatId = chatIdProp.GetString();

                // إرسال رسالة تجريبية صامتة للتحقق
                var payload = new { chat_id = chatId, text = "🔗 Emaily System: Telegram Integration Verified Successfully!", disable_notification = true };
                using var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsJsonAsync($"https://api.telegram.org/bot{_botToken}/sendMessage", payload);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                _logger.LogWarning(new Log(userId, "Telegram Integration Verification Failed"));
                return false;
            }
        }

        private async Task<bool> VerifyGoogleSheetsConfigAsync(Guid userId, string configJson)
        {
            try
            {
                // 1. التحقق من وجود الرابط في الـ JSON
                using var document = JsonDocument.Parse(configJson);
                if (!document.RootElement.TryGetProperty("sheetUrl", out var urlProp))
                {
                    return false;
                }

                string sheetUrl = urlProp.GetString()!;

                // 2. استخراج Spreadsheet ID
                var match = MyRegex().Match(sheetUrl);
                if (!match.Success)
                {
                    return false;
                }
                string spreadsheetId = match.Groups[1].Value;

                #pragma warning disable CS0618 // نخبر الـ Compiler أننا نثق في مصدر البيانات وأنه آمن (من متغيرات البيئة)

                var credential = GoogleCredential.FromJson(_jsonCredentials)
                                                 .CreateScoped(SheetsService.Scope.Spreadsheets);
                #pragma warning restore CS0618 // نعيد تفعيل التحذير لباقي أجزاء الكود لحمايته

                var service = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Emaily App Verification"
                });

                // 4. إجراء اختبار اتصال (جلب الـ ID الخاص بالشيت فقط لتقليل استهلاك البيانات)
                // إذا لم يكن الإيميل البرمجي يمتلك صلاحية على هذا الشيت، سيقوم جوجل برمي Exception هنا
                var request = service.Spreadsheets.Get(spreadsheetId);
                request.Fields = "spreadsheetId"; // تقليل حجم الرد (Optimization)

                await request.ExecuteAsync();

                return true;
            }
            catch
            {
                _logger.LogWarning(new Log(userId, "Google Sheets Integration Verification Failed"));
                return false;
            }
        }

        [GeneratedRegex(@"/d/([a-zA-Z0-9-_]+)")]
        private static partial Regex MyRegex();
    }
}