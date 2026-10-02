using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Emaily.BLL.DTOs;
using Emaily.BLL.DTOs.Service;

namespace Emaily.BLL.Interfaces
{
    public interface IServiceManager
    {
        Task<Result<IEnumerable<ServiceDto>>> GetAllServicesAsync(Guid userId);
        Task<Result<IEnumerable<ServiceDto>>> GetProjectServicesAsync(Guid userId, string projectId);
        Task<Result<ServiceDto>> AddServiceAsync(Guid userId, CreateServiceDto dto);
        Task<Result<ServiceDto>> UpdateServiceAsync(Guid userId, string serviceId, UpdateServiceDto dto);
        Task<bool> DeleteServiceAsync(Guid userId, string serviceId);
        Task<bool> ToggleStatusAsync(Guid userId, string serviceId, bool isActive);
        Task<Result<bool>> LinkServiceToProjectAsync(Guid userId, string serviceId, string projectId);
        Task<Result<bool>> UnlinkServiceFromProjectAsync(Guid userId, string serviceId, string projectId);
        Task<Result<bool>> UnlockServiceAsync(Guid userId, string serviceId);
    }
}