using Emaily.BLL.DTOs;
using Emaily.BLL.DTOs.Admin;
using Emaily.BLL.DTOs.Billing;
using Emaily.BLL.Interfaces;
using Emaily.DAL.Entities;
using Emaily.DAL.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Emaily.BLL.Helpers.Interfaces;

namespace Emaily.BLL.Services
{
    public class AdminService : IAdminService
    {
        private readonly IUnitOfWork _uow;
        private readonly IConfiguration _configuration;
        private readonly IBillingService _billingService;
        private readonly ILoggerService _logger;
        private readonly IBanManagerService _banManagerService;
        private readonly int _pageSize;

        public AdminService(IUnitOfWork uow, IConfiguration configuration, IBillingService billingService, IBanManagerService banManagerService, ILoggerService logger)
        {
            _uow = uow;
            _logger = logger;
            _configuration = configuration;
            _billingService = billingService;
            _banManagerService = banManagerService;
            _pageSize = _configuration.GetValue<int>("PageSize");
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync()
        {
            var users = await _uow.Users.FindAllAsync(u => !u.IsDeleted);
            var invoices = await _uow.Invoices.FindAllAsync(i => i.Status == Invoice.Statuses.Paid);
            var pendingSubmissions = await _uow.Submissions.FindAllAsync(s => s.Status == Submission.Statuses.Pending);
            var failedSubmissions = await _uow.Submissions.FindAllAsync(s => s.Status == Submission.Statuses.Failed);
            var activeUsers = users.Count(u => u.IsActive);
            var totalProjects = await _uow.Projects.FindAllAsync(p => !p.IsDeleted);

            return new DashboardStatsDto
            {
                TotalUsers = users.Count(),
                ActiveUsers = activeUsers,
                TotalRevenue = invoices.Sum(i => i.Amount),
                PendingSubmissions = pendingSubmissions.Count(),
                TotalProjects = totalProjects.Count(),
                FailedSubmissions = failedSubmissions.Count()
            };
        }

        public async Task<PagedResultDto<AdminUserDto>> GetUsersAsync(int page)
        {
            var (Items, TotalCount) = await _uow.Users.GetPagedAsync(u => !u.IsDeleted, page, _pageSize, u => u.FullName, true);

            return new PagedResultDto<AdminUserDto>
            {
                Items = Items.Select(u => new AdminUserDto
                {
                    Id = u.Id.ToString(),
                    FullName = u.FullName,
                    Email = u.Email,
                    IsActive = u.IsActive
                }),
                TotalCount = TotalCount,
                CurrentPage = page,
                PageSize = _pageSize
            };
        }

        public async Task<PagedResultDto<object>> GetAdminProjectsAsync(int page)
        {
            var (Items, TotalCount) = await _uow.Projects.GetPagedAsync(p => !p.IsDeleted, page, _pageSize, p => p.CreatedAt, true);

            return new PagedResultDto<object>
            {
                Items = Items.Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.UserId,
                    p.IsActive,
                    p.CreatedAt
                }),
                TotalCount = TotalCount,
                CurrentPage = page,
                PageSize = _pageSize
            };
        }

        public async Task<PagedResultDto<object>> GetAdminTemplatesAsync(int page)
        {
            var (Items, TotalCount) = await _uow.Templates.GetPagedAsync(t => !t.IsDeleted, page, _pageSize, t => t.CreatedAt, true);

            return new PagedResultDto<object>
            {
                Items = Items.Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.Subject,
                    t.CreatedAt,
                    t.IsActive
                }),
                TotalCount = TotalCount,
                CurrentPage = page,
                PageSize = _pageSize
            };
        }

        public async Task<PagedResultDto<object>> GetAdminServicesAsync(int page)
        {
            var (Items, TotalCount) = await _uow.Services.GetPagedAsync(s => !s.IsDeleted, page, _pageSize, s => s.FromEmail, true);

            return new PagedResultDto<object>
            {
                Items = Items.Select(s => new
                {
                    s.Id,
                    s.FromEmail,
                    s.ProviderType,
                    s.IsActive
                }),
                TotalCount = TotalCount,
                CurrentPage = page,
                PageSize = _pageSize
            };
        }

        public async Task<PagedResultDto<SystemLogDto>> GetSystemLogsAsync(int page)
        {
            var (Items, TotalCount) = await _uow.SystemLogs.GetPagedAsync(l => true, page, _pageSize, l => l.CreatedAt, true);

            return new PagedResultDto<SystemLogDto>
            {
                Items = Items.Select(l => new SystemLogDto
                {
                    Id = l.Id,
                    LogLevel = l.LogLevel,
                    ExecutionTrace = l.ExecutionTrace,
                    Message = l.Message,
                    ExceptionDetails = l.ExceptionDetails,
                    UserId = l.UserId?.ToString(),
                    ProjectId = l.ProjectId,
                    IpAddress = l.IpAddress,
                    CreatedAt = l.CreatedAt
                }),
                TotalCount = TotalCount,
                CurrentPage = page,
                PageSize = _pageSize
            };
        }

        public async Task<AdminUserDetailsDto?> GetUserDetailsAsync(string userId)
        {
            if (!Guid.TryParse(userId, out var parsedId)) return null;

            var user = await _uow.Users.FindAsync(u => u.Id == parsedId && !u.IsDeleted);
            if (user == null) return null;

            var activeSub = await _uow.Subscriptions.FindAsync(s => s.UserId == parsedId && s.Status == Subscription.Statuses.Active);
            var planName = "No Active Plan";
            if (activeSub != null)
            {
                var plan = await _uow.Plans.FindAsync(p => p.Id == activeSub.PlanId);
                planName = plan?.Name ?? "No Active Plan";
            }

            var projectsIDs = await _uow.Projects.SelectWhereAsync(selector: u => u.Id, criteria: p => p.UserId == parsedId && !p.IsDeleted);
            var servicesCount = await _uow.Services.CountAsync(s => parsedId == s.UserId);
            var templatesCount = await _uow.Templates.CountAsync(t => projectsIDs.Contains(t.ProjectId));


            return new AdminUserDetailsDto
            {
                Id = user.Id.ToString(),
                FullName = user.FullName,
                Email = user.Email,
                IsActive = user.IsActive,
                RemainingQuota = user.RemainingQuota,
                DevNotificationEmail = user.DevNotificationEmail,
                OverageEmails = user.OverageEmails,
                PlanName =  planName,
                ProjectsCount = projectsIDs.Count(),
                ServicesCount = servicesCount,
                TemplatesCount = templatesCount,
                Roles = string.Join(", ", user.Roles),
                CreatedAt = user.CreatedAt
            };
        }

        public async Task<bool> ToggleUserStatusAsync(string userId, bool isActive)
        {
            if (!Guid.TryParse(userId, out var parsedId)) return false;

            var user = await _uow.Users.FindAsync(u => u.Id == parsedId && !u.IsDeleted);
            if (user == null) return false;

            user.IsActive = isActive;
            _uow.Users.Update(user);
            await _uow.CompleteAsync();
            return true;
        }

        public async Task<PagedResultDto<object>> GetBannedListAsync(int page)
        {
            var (Items, TotalCount) = await _uow.Banned.GetPagedAsync(b => true, page, _pageSize, b => b.BannedUntil, true);

            return new PagedResultDto<object>
            {
                Items = Items.Select(b => new
                {
                    b.IpAddress,
                    b.BanLevel,
                    b.BannedUntil,
                }),
                TotalCount = TotalCount,
                CurrentPage = page,
                PageSize = _pageSize
            };
        }

        public async Task<object?> GetBanDetailsByIP(string IP)
        {
            var banDetails = await _uow.BanDetails.FindAllAsync(d => d.IpAddress == IP); 
            return banDetails;
        }

        public async Task<bool> AddBanAsync(AddBanDto dto, Guid adminId)
        {
            return await _banManagerService.ManualBanAsync(dto.IpAddress, dto.BannedUntil, dto.Reason, adminId);
        }

        public async Task<bool> RemoveBanAsync(BanDto dto, Guid adminId)
        {
            return await _banManagerService.PardonAsync(dto.IpAddress, dto.Reason, adminId);
        }

        public async Task<bool> ReduceBanAsync(BanDto dto, Guid adminId)
        {
            return await _banManagerService.ReduceBanLevelAsync(dto.IpAddress, dto.Reason, adminId);
        }

        public async Task<bool> CreateOrUpdatePlanAsync(CreatePlanDto dto)
        {
            var existingPlan = await _uow.Plans.FindAsync(p => p.Name == dto.Name);
            if (existingPlan != null)
            {
                // تحديث الخطة الحالية
                existingPlan.MonthlyPrice = dto.MonthlyPrice;
                existingPlan.MaxEmailsPerMonth = dto.MaxEmailsPerMonth;
                existingPlan.MaxProjects = dto.MaxProjects;
                existingPlan.MaxServices = dto.MaxServices;
                existingPlan.MaxTemplates = dto.MaxTemplates;
                existingPlan.MaxAttachmentsPerTemplate = dto.MaxAttachmentsPerTemplate;
                existingPlan.CanUseGoogleSheets = dto.CanUseGoogleSheets;
                existingPlan.CanUseAi = dto.CanUseAI;
                existingPlan.CanUseTelegramBot = dto.CanUseTelegramBot;
                _uow.Plans.Update(existingPlan);
            }
            else
            {
                // إنشاء خطة جديدة
                await _uow.Plans.AddAsync(new Plan
                {
                    Id = Guid.NewGuid(),
                    Name = dto.Name,
                    MonthlyPrice = dto.MonthlyPrice,
                    MaxEmailsPerMonth = dto.MaxEmailsPerMonth,
                    MaxProjects = dto.MaxProjects,
                    MaxServices = dto.MaxServices,
                    MaxTemplates = dto.MaxTemplates,
                    MaxAttachmentsPerTemplate = dto.MaxAttachmentsPerTemplate,
                    CanUseGoogleSheets = dto.CanUseGoogleSheets,
                    CanUseAi = dto.CanUseAI,
                    CanUseTelegramBot = dto.CanUseTelegramBot,
                    IsActive = true
                });
            }
            await _uow.CompleteAsync();
            return true;
        }

        public async Task<IEnumerable<object>> GetUserProjectsAsync(string userId)
        {
            if (!Guid.TryParse(userId, out var uid)) return [];
            var projects = await _uow.Projects.FindAllAsync(p => p.UserId == uid);
            return projects.Select(p => new { p.Id, p.Name, p.IsActive, p.CreatedAt });
        }

        public async Task<IEnumerable<object>> GetProjectTemplatesAsync(string projectId)
        {
            var templates = await _uow.Templates.FindAllAsync(t => t.ProjectId == projectId);
            return templates.Select(t => new { t.Id, t.Name, t.Subject, t.IsActive });
        }

        public async Task<IEnumerable<object>> GetProjectServicesAsync(string projectId)
        {
            var serviceIds = await _uow.ProjectServices.SelectWhereAsync(selector: ps => ps.ServiceId, criteria: ps => ps.ProjectId == projectId);
            var services = await _uow.Services.FindAllAsync(s => serviceIds.Contains(s.Id));
            return services.Select(s => new { s.Id, s.FromName, s.ProviderType, s.FromEmail, s.IsActive });
        }

        public async Task<object?> GetTemplateByIdAsync(string templateId)
        {
            var template = await _uow.Templates.FindAsync(t => t.Id == templateId);
            if (template == null) return null;

            return new { 
                template.Id, 
                template.Name, 
                template.Subject, 
                template.ToEmail, 
                template.DoSaveInHistory,
                template.EnableRecaptchaV2, 
                template.EnableAppCheck, 
                template.Issuer,
                template.EnableAutoReply,
                template.AutoReplyTemplateId,
                template.ReplyTo,
                template.Bcc,
                template.Cc,
                template.ProjectId, 
                template.IsActive };
        }

        public async Task<object?> GetServiceByIdAsync(string serviceId)
        {
            var service = await _uow.Services.FindAsync(s => s.Id == serviceId,
                includes: q => q.Include(s => s.ServiceApiKey)
                               .Include(s => s.ServiceAppPassword)
                               .Include(s => s.ServiceOauth));
            if (service == null) return null;
            
            switch (service.ProviderType)
            {
                case Service.ProviderTypes.ApiKey:
                    return new
                    {
                        service.Id,
                        service.UserId,
                        service.ProviderType,
                        service.FromEmail,
                        service.FromName,
                        service.ServiceApiKey!.ProviderName,
                        service.IsActive
                    };
                case Service.ProviderTypes.AppPassword:
                    return new
                    {
                        service.Id,
                        service.UserId,
                        service.ProviderType,
                        service.FromEmail,
                        service.FromName,
                        service.ServiceAppPassword!.SmtpHost,
                        service.ServiceAppPassword.SmtpPort,
                        service.ServiceAppPassword.Username,
                        service.IsActive
                    };
                case Service.ProviderTypes.OAuth:
                    return new
                    {
                        service.Id,
                        service.UserId,
                        service.ProviderType,
                        service.FromEmail,
                        service.FromName,
                        service.ServiceOauth!.OauthProvider,
                        service.ServiceOauth.TokenExpiry,
                        service.IsActive
                    };
                default:
                    _logger.LogWarning(new Log(service.UserId, $"Unknown provider type for service {service.Id}: {service.ProviderType}"));
                    return new
                    {
                        service.Id,
                        service.UserId,
                        service.ProviderType,
                        service.FromEmail,
                        service.FromName,
                        service.IsActive
                    };
            }
        }

        public async Task<AdminAnalyticsDto> GetSystemAnalyticsAsync()
        {
            var now = DateTime.UtcNow;
            var fourteenDaysAgo = now.Date.AddDays(-13); // لضمان جلب 14 يوم بالضبط

            var users = await _uow.Users.FindAllAsync(u => !u.IsDeleted);
            var projects = await _uow.Projects.FindAllAsync(p => !p.IsDeleted);
            var allSubmissions = await _uow.Submissions.GetAllAsync();

            var totalUsers = users.Count();
            var activeUsers = users.Count(u => u.IsActive);
            var suspendedUsers = totalUsers - activeUsers;
            var totalProjects = projects.Count();

            var totalSent = allSubmissions.Count(s => s.Status == Submission.Statuses.Sent || s.Status == Submission.Statuses.Resent);
            var totalFailed = allSubmissions.Count(s => s.Status == Submission.Statuses.Failed);
            var totalPending = allSubmissions.Count(s => s.Status == Submission.Statuses.Pending);

            var totalEmails = totalSent + totalFailed;
            var successRate = totalEmails > 0
                ? Math.Round(((double)totalSent / totalEmails) * 100, 1)
                : 0;

            //  حجم الإرسال اليومي خلال آخر 14 يومًا
            var recentSubmissions = allSubmissions
                .Where(s => s.ReceivedAt >= fourteenDaysAgo)
                .GroupBy(s => s.ReceivedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Sent = g.Count(s => s.Status == Submission.Statuses.Sent || s.Status == Submission.Statuses.Resent),
                    Failed = g.Count(s => s.Status == Submission.Statuses.Failed),
                    Pending = g.Count(s => s.Status == Submission.Statuses.Pending)
                })
                .ToList();

            var volumeByDay = new List<VolumeByDayDto>();
            for (int i = 13; i >= 0; i--)
            {
                var targetDate = now.Date.AddDays(-i);
                var dayData = recentSubmissions.FirstOrDefault(d => d.Date == targetDate);

                volumeByDay.Add(new VolumeByDayDto
                {
                    Date = targetDate.ToString("yyyy-MM-dd"),
                    Sent = dayData?.Sent ?? 0,
                    Failed = dayData?.Failed ?? 0,
                    Pending = dayData?.Pending ?? 0
                });
            }

            // 5. صحة النظام
            var systemHealth = new SystemHealthDto
            {
                Status = totalPending > 500 ? "Degraded" : "Operational",
                QueueDepth = totalPending,
                AvgDeliveryTimeMs = 240 // رقم تجريبي لحين حسابها برمجياً
            };

            // 6. الإيرادات والخطط (بيانات حقيقية عبر جدول الاشتراكات)
            var plans = await _uow.Plans.GetAllAsync();
            var activeSubscriptions = await _uow.Subscriptions.FindAllAsync(s => s.Status == Subscription.Statuses.Active);

            var revenueByPlan = plans.Select(plan =>
            {
                // نحسب عدد الاشتراكات النشطة المربوطة بهذه الخطة
                var subscriberCount = activeSubscriptions.Count(s => s.PlanId == plan.Id);

                return new RevenueByPlanDto
                {
                    PlanName = plan.Name,
                    SubscriberCount = subscriberCount,
                    MRR = subscriberCount * plan.MonthlyPrice // تأكد من اسم عمود السعر في جدول الخطط لديك
                };
            }).OrderByDescending(p => p.MRR).ToList();

            // 7. إرجاع النتيجة النهائية
            return new AdminAnalyticsDto
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                SuspendedUsers = suspendedUsers,
                TotalProjects = totalProjects,
                TotalEmailsSent = totalSent,
                TotalEmailsFailed = totalFailed,
                OverallSuccessRate = successRate,
                TotalMRR = revenueByPlan.Sum(p => p.MRR),
                RevenueByPlan = revenueByPlan,
                VolumeByDay = volumeByDay,
                SystemHealth = systemHealth
            };
        }

        public async Task<PagedResultDto<AdminUserDto>> SearchUsersAsync(string searchTerm, int page)
        {
            var (Items, TotalCount) = await _uow.Users.GetPagedAsync(u => u.FullName.Contains(searchTerm) || u.Email.Contains(searchTerm) || u.Id.ToString().Contains(searchTerm), page, _pageSize, u => u.FullName, true);
            return new PagedResultDto<AdminUserDto>
            {
                Items = Items.Select(u => new AdminUserDto
                {
                    Id = u.Id.ToString(),
                    FullName = u.FullName,
                    Email = u.Email,
                    IsActive = u.IsActive
                }),
                TotalCount = TotalCount,
                CurrentPage = page,
                PageSize = _pageSize
            };
        }

        public async Task<PagedResultDto<object>> SearchProjectsAsync(string searchTerm, int page)
        {
            var (Items, TotalCount) = await _uow.Projects.GetPagedAsync(p => p.Name.Contains(searchTerm) || p.Id.Contains(searchTerm), page, _pageSize, p => p.CreatedAt, true);
            return new PagedResultDto<object>
            {
                Items = Items.Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.UserId,
                    p.IsActive,
                    p.CreatedAt
                }),
                TotalCount = TotalCount,
                CurrentPage = page,
                PageSize = _pageSize
            };
        }

        public async Task<PagedResultDto<object>> SearchTemplatesAsync(string searchTerm, int page)
        {
            var (Items, TotalCount) = await _uow.Templates.GetPagedAsync(t => t.Name.Contains(searchTerm) || t.Id.Contains(searchTerm), page, _pageSize, t => t.CreatedAt, true);
            return new PagedResultDto<object>
            {
                Items = Items.Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.Subject,
                    t.IsActive,
                    t.CreatedAt
                }),
                TotalCount = TotalCount,
                CurrentPage = page,
                PageSize = _pageSize
            };
        }

        public async Task<PagedResultDto<object>> SearchServicesAsync(string searchTerm, int page)
        {
            var (Items, TotalCount) = await _uow.Services.GetPagedAsync(s => s.FromEmail.Contains(searchTerm) || s.Id.Contains(searchTerm) || s.ProviderType.Contains(searchTerm), page, _pageSize, s => s.FromEmail, true);
            return new PagedResultDto<object>
            {
                Items = Items.Select(s => new
                {
                    s.Id,
                    s.FromEmail,
                    s.ProviderType,
                    s.IsActive
                }),
                TotalCount = TotalCount,
                CurrentPage = page,
                PageSize = _pageSize
            };
        }

        public async Task<PagedResultDto<object>> SearchBannedListAsync(string searchTerm, int page)
        {
            var (Items, TotalCount) = await _uow.Banned.GetPagedAsync(b => b.IpAddress.Contains(searchTerm), page, _pageSize, b => b.BannedUntil, true);
            return new PagedResultDto<object>
            {
                Items = Items.Select(b => new
                {
                    b.IpAddress,
                    b.BanLevel,
                    b.BannedUntil
                }),
                TotalCount = TotalCount,
                CurrentPage = page,
                PageSize = _pageSize
            };
        }

        public async Task<PagedResultDto<SystemLogDto>> SearchSystemLogsAsync(string searchTerm, int page)
        {
            // 💡 ملاحظة هندسية (Architecture Note):
            // تم استخدام علامة (!) لإسكات تحذيرات الـ C# Compiler من احتمالية حدوث NullReferenceException.
            // بما أن هذا الـ LINQ سيتم ترجمته إلى استعلام SQL عبر Entity Framework Core،
            // فإن محرك قاعدة البيانات سيتعامل مع القيم الفارغة (Nulls) بأمان تام (Safe Evaluation)
            // ولن يتسبب في أي أخطاء (Exceptions) وقت التشغيل.

            var search = searchTerm.ToLower();

            var (Items, TotalCount) = await _uow.SystemLogs.GetPagedAsync(l =>
                l.Message!.Contains(search) ||
                l.ExecutionTrace!.Contains(search) ||
                l.ExceptionDetails!.Contains(search) ||
                l.UserId.ToString()!.Contains(search) ||
                l.ProjectId!.Contains(search) ||
                l.IpAddress!.Contains(search) ||
                l.LogLevel!.Contains(search),
                page,
                _pageSize,
                l => l.CreatedAt,
                true);  
            return new PagedResultDto<SystemLogDto>
            {
                Items = Items.Select(l => new SystemLogDto
                {
                    Id = l.Id,
                    LogLevel = l.LogLevel,
                    ExecutionTrace = l.ExecutionTrace,
                    Message = l.Message,
                    ExceptionDetails = l.ExceptionDetails,
                    UserId = l.UserId?.ToString(),
                    ProjectId = l.ProjectId,
                    IpAddress = l.IpAddress,
                    CreatedAt = l.CreatedAt
                }),
                TotalCount = TotalCount,
                CurrentPage = page,
                PageSize = _pageSize
            };
        }

        public async Task<Result<bool>> SubscribeAsync(AdminSubscribeRequestDto dto)
        {
            return await _billingService.SubscribeAsync(dto.UserId, new SubscribeRequestDto
            {
                PlanId = dto.PlanId
            });
        }
    }
}