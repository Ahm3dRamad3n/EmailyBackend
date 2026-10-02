using Emaily.DAL.Entities;
using Emaily.DAL.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Emaily.BLL.Helpers.Interfaces;

namespace Emaily.BLL.Helpers
{
    public class SubscriptionMonitorHelper(IServiceProvider serviceProvider, IConfiguration configuration) : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly string _frontendUrl = configuration["ClientUrl"] ?? throw new InvalidOperationException("ClientUrl environment variable is not set.");

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 1. تسجيل الإقلاع باستخدام نطاق (Scope) مؤقت وسريع
            using (var startupScope = _serviceProvider.CreateScope())
            {
                var startupLogger = startupScope.ServiceProvider.GetRequiredService<ILoggerService>();
                startupLogger.LogInfo(new DTOs.Log(Guid.Empty, "SubscriptionMonitorService started."));
            }

            // الحلقة التكرارية ستعمل طالما التطبيق شغال
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // النطاق الأساسي للعمليات
                    using var scope = _serviceProvider.CreateScope();
                    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailSenderService>();
                    var planManager = scope.ServiceProvider.GetRequiredService<IPlanManager>();

                    await ProcessThreeDaysReminders(uow, emailService);
                    await ProcessExpiredSubscriptions(uow, emailService, planManager);
                }
                catch (Exception ex)
                {
                    // 2. تسجيل الأخطاء الكارثية بإنشاء نطاق (Scope) مؤقت للوجر
                    using var errorScope = _serviceProvider.CreateScope();
                    var errorLogger = errorScope.ServiceProvider.GetRequiredService<ILoggerService>();
                    errorLogger.LogCritical(new DTOs.Log(Guid.Empty, "Error in SubscriptionMonitorService: " + ex.Message, exceptionDetails: ex.ToString()));
                }

                // جعل الـ Worker ينام لمدة 24 ساعة قبل الفحص التالي
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }

        // =================================================================================
        // 1. هندلة إشعارات الـ 3 أيام قبل الانتهاء للخطط المدفوعة
        // =================================================================================
        private async Task ProcessThreeDaysReminders(IUnitOfWork uow, IEmailSenderService emailService)
        {
            var targetDate = DateTime.UtcNow.AddDays(3).Date;

            // جلب الاشتراكات الفعالة التي تنتهي بعد 3 أيام تحديداً، والتابعة لخطط غير مجانية
            var upcomingExpirations = await uow.Subscriptions.FindAllAsync(s =>
                s.Status == Subscription.Statuses.Active &&
                s.EndDate.Date == targetDate &&
                s.Plan.MonthlyPrice > 0, // افتراض أن الخطط المدفوعة سعرها أكبر من صفر
                includes: s => s.Include(u => u.User).Include(p => p.Plan));

            string frontendBillingUrl = _frontendUrl + "/dashboard/billing";

            foreach (var sub in upcomingExpirations)
            {
                string fullHtmlEmail = EmailTemplateBuilder.GenerateSubscriptionExpiringTemplate(
                    sub.User.FullName,
                    sub.Plan.Name,
                    sub.EndDate,
                    frontendBillingUrl
                );

                var response = await emailService.SendWithSystemAsync(sub.User.FullName, sub.User.DevNotificationEmail, "System Alert: Your subscription expires in 3 days", fullHtmlEmail, CancellationToken.None, null);
                if (!response.Success)
                {
                    using var errorScope = _serviceProvider.CreateScope();
                    var errorLogger = errorScope.ServiceProvider.GetRequiredService<ILoggerService>();
                    errorLogger.LogError(new DTOs.Log(sub.User.Id, $"Failed to send subscription expiration reminder email: {response.ErrorMessage}"));
                }
            }
        }

        // =================================================================================
        // 2. هندلة انتهاء الخطة، إرسال الإيميل بالخسائر، والتحويل للخطة المجانية
        // =================================================================================
        private async Task ProcessExpiredSubscriptions(IUnitOfWork uow, IEmailSenderService emailService, IPlanManager planManager)
        {
            var today = DateTime.UtcNow;

            // جلب الاشتراكات الفعالة التي انتهى وقتها
            var expiredSubscriptions = await uow.Subscriptions.FindAllAsync(s =>
                s.Status == Subscription.Statuses.Active &&
                s.EndDate <= today,
                includes: s => s.Include(u => u.User).Include(p => p.Plan));

            if (!expiredSubscriptions.Any()) return;

            // جلب الخطة المجانية من الداتا بيز (بافتراض أن سعرها 0 أو اسمها Free)
            var freePlan = await uow.Plans.FindAsync(p => p.MonthlyPrice == 0 && p.IsActive) ?? throw new InvalidOperationException("Free plan not found in the database. Please ensure a Free plan exists.");
            string frontendBillingUrl = _frontendUrl + "/dashboard/billing";

            foreach (var sub in expiredSubscriptions)
            {
                var user = sub.User;
                int overageEmails = user.RemainingQuota; // الإيميلات المتبقية التي سيخسرها المستخدم

                string fullHtmlEmail = EmailTemplateBuilder.GenerateSubscriptionDowngradedTemplate(
                    sub.User.FullName,
                    sub.Plan.Name,
                    overageEmails, 
                    frontendBillingUrl
                );

                var response = await emailService.SendWithSystemAsync(sub.User.FullName, sub.User.DevNotificationEmail, "Notice: Account downgraded to Free plan", fullHtmlEmail, CancellationToken.None, null);
                if (!response.Success)
                {
                    using var errorScope = _serviceProvider.CreateScope();
                    var errorLogger = errorScope.ServiceProvider.GetRequiredService<ILoggerService>();
                    errorLogger.LogError(new DTOs.Log(sub.User.Id, $"Failed to send subscription downgrade email: {response.ErrorMessage}"));
                }

                // 2. إيقاف الاشتراك القديم
                sub.Status = Subscription.Statuses.Expired;
                sub.RevokeAt = DateTime.UtcNow;
                uow.Subscriptions.Update(sub);

                // 3. إنشاء اشتراك جديد بالخطة المجانية
                var newFreeSubscription = new Subscription
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    PlanId = freePlan.Id,
                    Status = Subscription.Statuses.Active,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddMonths(1), // تجديد شهري للخطة المجانية
                    CreatedAt = DateTime.UtcNow
                };
                await uow.Subscriptions.AddAsync(newFreeSubscription);

                // 4. تحديث رصيد المستخدم للحد الخاص بالخطة المجانية
                user.RemainingQuota = freePlan.MaxEmailsPerMonth;
                uow.Users.Update(user);

                await planManager.ApplyPlanLimitsAsync(user.Id, freePlan);
            }

            // حفظ كل التعديلات في قاعدة البيانات دفعة واحدة
            await uow.CompleteAsync();
        }
    }
}