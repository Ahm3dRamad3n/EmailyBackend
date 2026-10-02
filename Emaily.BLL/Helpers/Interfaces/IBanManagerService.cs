using Microsoft.Extensions.Caching.Memory;
using Hangfire;

namespace Emaily.BLL.Helpers.Interfaces
{
    public interface IBanManagerService
    {
        bool IsBanned(string identifier);
        Task RecordViolationAsync(string identifier, string reason, int weight, string userAgent, string endpoint);

        // دوال الإدارة (Admin Panel)
        Task<bool> ManualBanAsync(string identifier, DateTime bannedUntil, string reason, Guid adminId);
        Task<bool> PardonAsync(string identifier, string reason, Guid adminId);
        Task<bool> ReduceBanLevelAsync(string identifier, string reason, Guid adminId);
    }
}