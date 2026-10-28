using Emaily.BLL.DTOs;
using Emaily.BLL.Helpers.Interfaces;
using Emaily.DAL.Entities;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text;

namespace Emaily.BLL.Helpers.Services
{
    public enum TargetEndpointEnum
    {
        NewSubmission,
        VerifyTelegramConfig
    }

    public class WebhookSenderService : IWebhookSenderService
    {
        private readonly string _masterKey;
        private readonly string _botWebhookUrl;
        private readonly ILoggerService _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public WebhookSenderService(IHttpClientFactory httpClientFactory, IConfiguration config, ILoggerService logger)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _botWebhookUrl = config["BOT_WEBHOOK_URL"] ?? throw new Exception("BOT_WEBHOOK_URL is not configured in appsettings.json or environment variables.");
            _masterKey = config["INTERNALM2M_MASTER_KEY"] ?? throw new Exception("INTERNALM2M_MASTER_KEY is not configured in appsettings.json or environment variables.");
        }

        public async Task<bool> NotifyBotAsync(TargetEndpointEnum targetEndpoint, BotNotificationPayload payload, Guid userId)
        {
            string fullWebhookUrl = targetEndpoint switch
            {
                TargetEndpointEnum.NewSubmission => $"{_botWebhookUrl.TrimEnd('/')}/api/webhooks/new-submission",
                TargetEndpointEnum.VerifyTelegramConfig => $"{_botWebhookUrl.TrimEnd('/')}/api/webhooks/verify-config",
                _ => throw new ArgumentOutOfRangeException(nameof(targetEndpoint), "Endpoint is not supported.")
            };

            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            client.DefaultRequestHeaders.Add("x-internal-key", _masterKey);

            var response = await client.PostAsJsonAsync(fullWebhookUrl, payload);

            if (!response.IsSuccessStatusCode)
            {
                string errorResponse = await response.Content.ReadAsStringAsync();

                _logger.LogWarning(new Log(userId, $"Internal bot webhook failed for {fullWebhookUrl}. Status: {response.StatusCode}. Details: {errorResponse}"));
                return false;
            }

            return true;
        }
    }
}