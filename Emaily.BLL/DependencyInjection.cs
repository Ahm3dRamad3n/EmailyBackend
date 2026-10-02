using Emaily.BLL.Helpers;
using Emaily.BLL.Helpers.Interfaces;
using Emaily.BLL.Helpers.Services;
using Emaily.BLL.Interfaces;
using Emaily.BLL.Services;
using Emaily.DAL;
using Emaily.DAL.Entities;
using Emaily.DAL.Interfaces;
using Emaily.DAL.Repositories;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Emaily.BLL
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddBusinessLogicLayer(this IServiceCollection services, IConfiguration configuration)
        {
            string connectionString = configuration["DB_CONNECTION_STRING"] ?? throw new InvalidOperationException("DB_CONNECTION_STRING environment variable is not set.");

            services.AddDbContext<EmailyDbContext>(options =>
                options.UseSqlServer(connectionString));

            services.AddExceptionHandler<GlobalExceptionHandler>();
            services.AddProblemDetails();
            services.AddHttpClient();
            services.AddHttpContextAccessor();
            services.AddHangfire(config => config
           .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
           .UseSimpleAssemblyNameTypeSerializer()
           .UseRecommendedSerializerSettings()
           .UseSqlServerStorage(connectionString));
            services.AddHangfireServer();
            services.AddMemoryCache();
            services.AddSingleton<LogQueue>();
            services.AddHostedService<SubscriptionMonitorHelper>();
            services.AddHostedService<LogDatabaseProcessor>();
            services.AddHostedService<BanCacheWarmupHelper>();
            services.AddScoped<IEncryptionHelper, EncryptionHelper>();
            services.AddScoped<ILoggerService, LoggerService>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IBanManagerService, BanManagerService>();
            services.AddScoped<IAttachmentManager, AttachmentManager>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IProjectsService, ProjectsService>();
            services.AddScoped<IIntegrationService, IntegrationService>();
            services.AddScoped<IServiceManager, ServiceManager>();
            services.AddScoped<ISubmissionService, SubmissionService>();
            services.AddScoped<IEmailSenderService, EmailSenderService>();
            services.AddScoped<ITemplateService, TemplateService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAnalyticsService, AnalyticsService>();
            services.AddScoped<IPlanManager, PlanManager>();
            services.AddScoped<IBillingService, BillingService>();
            services.AddScoped<IAdminService, AdminService>();

            return services;
        }
    }
}