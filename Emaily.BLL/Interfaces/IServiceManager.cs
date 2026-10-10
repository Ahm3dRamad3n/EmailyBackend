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
        Task<Result<IEnumerable<ServiceDto>>> GetProjectServicesAsync(string projectId);
        Task<Result<ServiceDto>> AddServiceAsync(Guid userId, CreateServiceDto dto);
        Task<Result<ServiceDto>> UpdateServiceAsync(string serviceId, UpdateServiceDto dto);
        Task<bool> DeleteServiceAsync(string serviceId);
        Task<bool> ToggleStatusAsync(string serviceId, bool isActive);
        Task<Result<bool>> LinkServiceToProjectAsync(string serviceId, string projectId);
        Task<Result<bool>> UnlinkServiceFromProjectAsync(string serviceId, string projectId);
        Task<Result<bool>> UnlockServiceAsync(string serviceId);
    }
}