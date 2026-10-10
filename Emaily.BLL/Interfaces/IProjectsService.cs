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
        Task<Result<ProjectDetailsDto>> GetByIdAsync(string projectId);
        Task<Result<ProjectDetailsDto>> UpdateAsync(string projectId, UpdateProjectDto dto);
        Task<Result<ProjectDetailsDto>> RegenerateKeysAsync(string projectId);
        Task<bool> DeleteAsync(string projectId);
        Task<bool> ToggleStatusAsync(string projectId, bool isActive);
        Task<Result<bool>> ChangeAccessModeAsync(string projectId, ChangeAccessModeDto dto);
        Task<Result<bool>> UnlockProjectAsync(string projectId);
    }
}