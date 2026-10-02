using Emaily.BLL.DTOs;
using Emaily.BLL.DTOs.Admin;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Emaily.BLL.Interfaces
{
    public interface IAdminService
    {
        Task<DashboardStatsDto> GetDashboardStatsAsync();

        // Users
        Task<PagedResultDto<AdminUserDto>> GetUsersAsync(int page);
        Task<AdminUserDetailsDto?> GetUserDetailsAsync(string userId);
        Task<bool> ToggleUserStatusAsync(string userId, bool isActive);

        // Banned
        Task<PagedResultDto<object>> GetBannedListAsync(int page);
        Task<object?> GetBanDetailsByIP(string IP);
        Task<bool> AddBanAsync(AddBanDto dto, Guid adminId);
        Task<bool> RemoveBanAsync(BanDto dto, Guid adminId);
        Task<bool> ReduceBanAsync(BanDto dto, Guid adminId);
        // الخطط
        Task<bool> CreateOrUpdatePlanAsync(CreatePlanDto dto);

        // الاستعلامات الشرطية (ترجع العدد أو البيانات)
        Task<PagedResultDto<object>> GetAdminProjectsAsync(int page);
        Task<PagedResultDto<object>> GetAdminTemplatesAsync(int page);
        Task<PagedResultDto<object>> GetAdminServicesAsync(int page);
        Task<PagedResultDto<SystemLogDto>> GetSystemLogsAsync(int page);
        // البحث في الاستعلامات الشرطية
        Task<PagedResultDto<AdminUserDto>> SearchUsersAsync(string searchTerm, int page);
        Task<PagedResultDto<object>> SearchProjectsAsync(string searchTerm, int page);
        Task<PagedResultDto<object>> SearchTemplatesAsync(string searchTerm, int page);
        Task<PagedResultDto<object>> SearchServicesAsync(string searchTerm, int page);
        Task<PagedResultDto<object>> SearchBannedListAsync(string searchTerm, int page);
        Task<PagedResultDto<SystemLogDto>> SearchSystemLogsAsync(string searchTerm, int page);

        // جلب العلاقات
        Task<IEnumerable<object>> GetUserProjectsAsync(string userId);
        Task<IEnumerable<object>> GetProjectTemplatesAsync(string projectId);
        Task<IEnumerable<object>> GetProjectServicesAsync(string projectId);

        // جلب التفاصيل
        Task<object?> GetTemplateByIdAsync(string templateId);
        Task<object?> GetServiceByIdAsync(string serviceId);

        // الإحصائيات الشاملة
        Task<AdminAnalyticsDto> GetSystemAnalyticsAsync();

        // تفعيل اشتراك لمستخدم
        Task<Result<bool>> SubscribeAsync(AdminSubscribeRequestDto dto);
    }
}