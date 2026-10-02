using Emaily.BLL.DTOs;
using Emaily.BLL.DTOs.Project;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Emaily.BLL.Interfaces
{
    public interface IProjectsService
    {
        Task<IEnumerable<ProjectDto>> GetAllProjectsAsync(Guid userId);
        Task<Result<ProjectDetailsDto>> CreateAsync(Guid userId, CreateProjectDto dto);
        Task<Result<ProjectDetailsDto>> GetByIdAsync(Guid userId, string projectId);
        Task<Result<ProjectDetailsDto>> UpdateAsync(Guid userId, string projectId, UpdateProjectDto dto);
        Task<Result<ProjectDetailsDto>> RegenerateKeysAsync(Guid userId, string projectId);
        Task<bool> DeleteAsync(Guid userId, string projectId);
        Task<bool> ToggleStatusAsync(Guid userId, string projectId, bool isActive);
        Task<Result<bool>> ChangeAccessModeAsync(Guid userId, string projectId, ChangeAccessModeDto dto);
        Task<Result<bool>> UnlockProjectAsync(Guid userId, string projectId);
    }
}