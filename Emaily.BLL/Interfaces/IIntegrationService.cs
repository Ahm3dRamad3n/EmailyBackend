using Emaily.BLL.DTOs;
using Emaily.BLL.DTOs.Integration;
using System;
using System.Threading.Tasks;

namespace Emaily.BLL.Interfaces
{
    public interface IIntegrationService
    {
        Task<Result<IEnumerable<IntegrationDto>>> GetIntegrationsAsync(string projectId);
        Task<Result<IntegrationDto>> AddIntegrationAsync(Guid userId, string projectId, CreateIntegrationDto dto);
        Task<Result<IntegrationDto>> UpdateIntegrationAsync(Guid userId, string integrationId, UpdateIntegrationDto dto);
        Task<bool> RemoveIntegrationAsync(string integrationId);
        Task<bool> ToggleStatusAsync(string integrationId, bool isActive);
        Task<IEnumerable<Guid>> GetUsersIdByChatIdAsync(string chatId);
        Task<IEnumerable<string>> GetProjectsIdByChatIdAndUserIdAsync(string chatId, Guid userId);
    }
}