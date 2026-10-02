using Emaily.BLL.Helpers.Interfaces;
using Emaily.DAL.Entities;
using Emaily.DAL.Interfaces;
using Hangfire;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using static Emaily.DAL.Entities.BanDetail;

namespace Emaily.BLL.Helpers.Services
{
    public class BanManagerService(IMemoryCache cache, IServiceScopeFactory scopeFactory, IBackgroundJobClient backgroundJobs) : IBanManagerService
    {
        private readonly IMemoryCache _cache = cache;
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        private readonly IBackgroundJobClient _backgroundJobs = backgroundJobs;
        private const double TOLERANCE_THRESHOLD = 5.0; // الحد الأقصى للنقاط قبل الحظر

        public bool IsBanned(string identifier)
        {
            return _cache.TryGetValue($"Ban_{identifier}", out _);
        }

        public async Task RecordViolationAsync(string identifier, string reason, int weight, string userAgent, string endpoint)
        {
            var pointsKey = $"Points_{identifier}";

            double currentPoints = _cache.GetOrCreate(pointsKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2);
                return 0.0;
            });

            currentPoints += (weight / TOLERANCE_THRESHOLD);
            _cache.Set(pointsKey, currentPoints, TimeSpan.FromHours(2));

            if (currentPoints >= TOLERANCE_THRESHOLD)
            {
                await ApplyAutoBanAsync(identifier, currentPoints, reason, weight, userAgent, endpoint);
                _cache.Remove(pointsKey);
            }
        }
        
        private async Task ApplyAutoBanAsync(string identifier, double totalPoints, string reason, int weight, string userAgent, string endpoint)
        {
            using var scope = _scopeFactory.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var record = await uow.Banned.FindAsync(b => b.IpAddress == identifier);

            int newLevel = record != null ? Math.Min(8, record.BanLevel + 1) : 1;

            var banDuration = GetBanDuration(newLevel);
            var bannedUntil = DateTime.UtcNow.Add(banDuration);

            _cache.Set($"Ban_{identifier}", true, banDuration);

            if (record == null)
            {
                await uow.Banned.AddAsync(new Banned { IpAddress = identifier, BanLevel = newLevel, BannedUntil = bannedUntil });
            }
            else
            {
                record.BanLevel = newLevel;
                record.BannedUntil = bannedUntil;
                uow.Banned.Update(record);
            }

            await uow.CompleteAsync();

            string fullReason = $"Auto-Ban triggered. Reached {totalPoints} points. Last reason: {reason}";
            await LogActionAsync(identifier, ActionTypes.AutoBan, weight, fullReason, userAgent, endpoint, null);

            if (banDuration > TimeSpan.FromHours(24))
            {
                string body = EmailTemplateBuilder.GenerateBanLevelTemplate(identifier, newLevel, fullReason);
                _backgroundJobs.Enqueue<IEmailSenderService>(email =>
                    email.NotifyAdminAsync("Auto-Ban Triggered", body, null, CancellationToken.None, null));
            }
        }

        private static TimeSpan GetBanDuration(int level)
        {
            return level switch
            {
                1 => TimeSpan.FromMinutes(1),
                2 => TimeSpan.FromMinutes(5),
                3 => TimeSpan.FromMinutes(15),
                4 => TimeSpan.FromHours(1),
                5 => TimeSpan.FromHours(6),
                6 => TimeSpan.FromHours(24),
                7 => TimeSpan.FromDays(7),
                8 => TimeSpan.FromDays(30),
                _ => TimeSpan.Zero
            };
        }

        private async Task LogActionAsync(string ip, string actionType, int weight, string reason, string userAgent, string endpoint, Guid? adminId)
        {
            using var scope = _scopeFactory.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            await uow.BanDetails.AddAsync(new BanDetail
            {
                IpAddress = ip,
                ActionType = actionType.ToString(), 
                ViolationWeight = weight,
                Reason = reason,
                UserAgent = userAgent,
                Endpoint = endpoint,
                AdminId = adminId,
                CreatedAt = DateTime.UtcNow
            });
            await uow.CompleteAsync();
        }

        // ================= دوال الأدمن =================
        public async Task<bool> PardonAsync(string identifier, string reason, Guid adminId)
        {
            using var scope = _scopeFactory.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var record = await uow.Banned.FindAsync(b => b.IpAddress == identifier);
            if (record != null)
            {
                record.BanLevel = 0;
                record.BannedUntil = DateTime.UtcNow.AddMinutes(-1); // جعلها في الماضي
                uow.Banned.Update(record);
                await uow.CompleteAsync();
            }

            _cache.Remove($"Ban_{identifier}");
            _cache.Remove($"Points_{identifier}");

            await LogActionAsync(identifier, ActionTypes.Pardon, 0, reason, "AdminPanel", "N/A", adminId);
            return true;
        }

        public async Task<bool> ManualBanAsync(string identifier, DateTime bannedUntil, string reason, Guid adminId)
        {
            using var scope = _scopeFactory.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var record = await uow.Banned.FindAsync(b => b.IpAddress == identifier);
            if (record == null)
            {
                await uow.Banned.AddAsync(new Banned { IpAddress = identifier, BanLevel = 8, BannedUntil = bannedUntil });
            }
            else
            {
                record.BanLevel = 8;
                record.BannedUntil = bannedUntil;
                uow.Banned.Update(record);
            }

            await uow.CompleteAsync();

            var timeRemaining = bannedUntil - DateTime.UtcNow;
            if (timeRemaining > TimeSpan.Zero)
            {
                _cache.Set($"Ban_{identifier}", true, timeRemaining);
            }

            await LogActionAsync(identifier, ActionTypes.ManualBan, 0, reason, "AdminPanel", "N/A", adminId);
            return true;
        }

        public async Task<bool> ReduceBanLevelAsync(string identifier, string reason, Guid adminId)
        {

            using var scope = _scopeFactory.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var record = await uow.Banned.FindAsync(b => b.IpAddress == identifier);
            if (record == null)
            {
                return false; 
            }

            int targetLevel = record.BanLevel - 1;

            if (targetLevel <= 0)
            {
                await PardonAsync(identifier, reason, adminId);
                return true;
            }

            record.BanLevel = targetLevel;
            var newDuration = GetBanDuration(targetLevel);
            record.BannedUntil = DateTime.UtcNow.Add(newDuration);

            uow.Banned.Update(record);
            await uow.CompleteAsync();

            if (newDuration > TimeSpan.Zero)
            {
                _cache.Set($"Ban_{identifier}", true, newDuration);
            }
            else
            {
                _cache.Remove($"Ban_{identifier}");
            }

            string fullReason = $"Ban level reduced from {targetLevel + 1} to {targetLevel}. Reason: {reason}";
            await LogActionAsync(identifier, ActionTypes.LevelReduced, 0, fullReason, "AdminPanel", "N/A", adminId);
            return true;
        }
    }
}