using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Emaily.DAL.Interfaces;

namespace Emaily.BLL.Helpers
{
    public class BanCacheWarmupHelper(IServiceScopeFactory scopeFactory, IMemoryCache cache) : IHostedService
    {
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        private readonly IMemoryCache _cache = cache;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var activeBans = await uow.Banned.FindAllAsync(b => b.BannedUntil > DateTime.UtcNow);

            foreach (var ban in activeBans)
            {
                var timeRemaining = ban.BannedUntil - DateTime.UtcNow;

                if (timeRemaining > TimeSpan.Zero)
                {
                    _cache.Set($"Ban_{ban.IpAddress}", true, timeRemaining);
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}