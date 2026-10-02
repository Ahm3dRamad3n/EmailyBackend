using Emaily.BLL.DTOs;
using Emaily.BLL.DTOs.Integration;
using System;
using System.Threading.Tasks;

namespace Emaily.BLL.Interfaces
{
    public interface IIntegrationService
    {
        Task<Result<IEnumerable<IntegrationDto>>> GetIntegrationsAsync(Guid userId, string projectId);
        Task<Result<IntegrationDto>> AddIntegrationAsync(Guid userId, string projectId, CreateIntegrationDto dto);
        Task<Result<IntegrationDto>> UpdateIntegrationAsync(Guid userId, string integrationId, UpdateIntegrationDto dto);
        Task<bool> RemoveIntegrationAsync(Guid userId, string integrationId);
        Task<bool> ToggleStatusAsync(Guid userId, string integrationId, bool isActive);
    }
}