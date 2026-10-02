using Emaily.BLL.DTOs.Analytics;
using Emaily.BLL.DTOs;
using System;
using System.Threading.Tasks;

namespace Emaily.BLL.Interfaces
{
    public interface IAnalyticsService
    {
        Task<OverviewAnalyticsDto> GetOverviewAnalyticsAsync(Guid userId);
        Task<Result<ProjectAnalyticsDto>> GetProjectAnalyticsAsync(Guid userId, string projectId);
    }
}