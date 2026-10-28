using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emaily.BLL.DTOs;
using Emaily.BLL.Helpers.Services;
using System.Threading.Tasks;

namespace Emaily.BLL.Helpers.Interfaces
{
    public interface IWebhookSenderService
    {
        Task<bool> NotifyBotAsync(TargetEndpointEnum targetEndpoint, BotNotificationPayload payload, Guid userId);
    }
}
