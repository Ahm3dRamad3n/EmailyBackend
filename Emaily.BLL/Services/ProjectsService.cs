using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using Emaily.BLL.DTOs;
using Emaily.BLL.DTOs.Project;
using Emaily.BLL.Helpers.Interfaces;
using Emaily.BLL.Helpers.Services;
using Emaily.BLL.Interfaces;
using Emaily.DAL.Entities;
using Emaily.DAL.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Emaily.BLL.Services
{
    public class ProjectsService(IUnitOfWork uow, IAttachmentManager am) : IProjectsService
    {
        private readonly IUnitOfWork _uow = uow;
        private readonly IAttachmentManager _am = am;

        public async Task<IEnumerable<ProjectDto>> GetAllProjectsAsync(Guid userId)
        {
            var projects = await _uow.Projects.FindAllAsync(p => p.UserId == userId && !p.IsDeleted);

            return projects.Select(p => new ProjectDto
            {
                Id = p.Id,
                Name = p.Name,
                IsActive = p.IsActive,
                IsLocked = p.IsLocked,
                CreatedAt = p.CreatedAt
            }).OrderByDescending(p => p.CreatedAt);
        }

        public async Task<Result<ProjectDetailsDto>> CreateAsync(Guid userId, CreateProjectDto dto)
        {
            // 1. التحقق من الحد الأقصى للمشاريع بناءً على اشتراك المستخدم الفعال
            var subscriptions = await _uow.Subscriptions.FindAllAsync(s => s.UserId == userId && s.Status == Subscription.Statuses.Active && s.EndDate > DateTime.UtcNow);
            var activeSubscription = subscriptions.OrderByDescending(s => s.CreatedAt).FirstOrDefault();

            if (activeSubscription == null)
                return Result<ProjectDetailsDto>.Failure("No active subscription found. Please subscribe to a plan to create a project.", StatusCodes.Status403Forbidden);

            var plan = await _uow.Plans.FindAsync(p => p.Id == activeSubscription.PlanId);
            if (plan == null)
                return Result<ProjectDetailsDto>.Failure("Plan associated with the active subscription not found.", StatusCodes.Status404NotFound);

            var currentProjects = await _uow.Projects.FindAllAsync(p => p.UserId == userId && !p.IsDeleted);

            if (currentProjects.Count() >= plan.MaxProjects)
                return Result<ProjectDetailsDto>.Failure($"You have reached the maximum number of projects allowed for your current plan ({plan.MaxProjects}). Please upgrade your plan to create more projects.", StatusCodes.Status403Forbidden);

            var project = new Project
            {
                Id = "p_" + Guid.NewGuid().ToString("N"),
                UserId = userId,
                Name = dto.Name,
                PublicApiKey = "pub_" + Guid.NewGuid().ToString("N"),
                PrivateApiKey = "sec_" + Guid.NewGuid().ToString("N"),
                RestrictedDomains = dto.RestrictedDomains,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                IsDeleted = false
            };

            await _uow.Projects.AddAsync(project);
            await _uow.CompleteAsync();

            return Result<ProjectDetailsDto>.Success(MapToDetailsDto(project));
        }

        public async Task<bool> ToggleStatusAsync(string projectId, bool isActive)
        {
            var project = await _uow.Projects.FindAsync(p => p.Id == projectId);
            if (project == null) return false;

            project.IsActive = isActive;
            _uow.Projects.Update(project);
            await _uow.CompleteAsync();
            return true;
        }

        public async Task<Result<ProjectDetailsDto>> GetByIdAsync(string projectId)
        {
            var project = await _uow.Projects.FindAsync(p => p.Id == projectId);
            if (project == null) return Result<ProjectDetailsDto>.Failure("Project not found.", StatusCodes.Status404NotFound);

            return Result<ProjectDetailsDto>.Success(MapToDetailsDto(project));
        }

        public async Task<Result<ProjectDetailsDto>> UpdateAsync(string projectId, UpdateProjectDto dto)
        {
            var project = await _uow.Projects.FindAsync(p => p.Id == projectId);
            if (project == null) return Result<ProjectDetailsDto>.Failure("Project not found.", StatusCodes.Status404NotFound);

            if (project.IsLocked)
                return Result<ProjectDetailsDto>.Failure("This project is locked due to plan limitations. Please upgrade your plan to unlock it.", StatusCodes.Status403Forbidden);

            project.Name = dto.Name;
            project.RestrictedDomains = dto.RestrictedDomains;

            _uow.Projects.Update(project);
            await _uow.CompleteAsync();

            return Result<ProjectDetailsDto>.Success(MapToDetailsDto(project));
        }

        public async Task<Result<ProjectDetailsDto>> RegenerateKeysAsync(string projectId)
        {
            var project = await _uow.Projects.FindAsync(p => p.Id == projectId);
            if (project == null) return Result<ProjectDetailsDto>.Failure("Project not found.", StatusCodes.Status404NotFound);

            if (project.IsLocked)
                return Result<ProjectDetailsDto>.Failure("This project is locked due to plan limitations. Please upgrade your plan to unlock it.", StatusCodes.Status403Forbidden);

            project.PublicApiKey = "pub_" + Guid.NewGuid().ToString("N");
            project.PrivateApiKey = "sec_" + Guid.NewGuid().ToString("N");

            _uow.Projects.Update(project);
            await _uow.CompleteAsync();

            return Result<ProjectDetailsDto>.Success(MapToDetailsDto(project));
        }

        public async Task<bool> DeleteAsync(string projectId)
        {
            var project = await _uow.Projects.FindAsync(
                criteria: p => p.Id == projectId,

                includes: q => q.Include(p => p.Integrations)
                                .Include(p => p.ProjectServices)
                                .Include(p => p.Templates)
                                    .ThenInclude(t => t.TemplateAttachments) 
            );

            if (project == null)
                return false;

            project.IsDeleted = true;
            project.IsActive = false;

            foreach (var integration in project.Integrations)
            {
                integration.IsDeleted = true;
                integration.IsActive = false;
                _uow.Integrations.Update(integration);
            }

            foreach (var projectService in project.ProjectServices)
            {
                _uow.ProjectServices.Delete(projectService);
            }

            foreach (var template in project.Templates)
            {
                template.IsDeleted = true;
                template.IsActive = false;
                _uow.Templates.Update(template);

                foreach (var attachment in template.TemplateAttachments)
                {
                    _am.Delete(attachment.FileUrl, AttachmentManager.AttachmentSource.Template);
                    _uow.TemplateAttachments.Delete(attachment);
                }
            }

            _uow.Projects.Update(project);
            await _uow.CompleteAsync();

            return true;
        }

        public async Task<Result<bool>> ChangeAccessModeAsync(string projectId, ChangeAccessModeDto dto)
        {
            var project = await _uow.Projects.FindAsync(p => p.Id == projectId);
            if (project == null) return Result<bool>.Failure("Project not found.", StatusCodes.Status404NotFound);

            if (project.IsLocked)
                return Result<bool>.Failure("This project is locked due to plan limitations. Please upgrade your plan to unlock it.", StatusCodes.Status403Forbidden);

            project.ProjectAccessMode = dto.AccessMode;
            _uow.Projects.Update(project);
            await _uow.CompleteAsync();
            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> UnlockProjectAsync(string projectId)
        {
            var project = await _uow.Projects.FindAsync(p => p.Id == projectId);

            if (project == null)
                return Result<bool>.Failure("Project not found.", StatusCodes.Status404NotFound);

            if (!project.IsLocked)
                return Result<bool>.Success(true);

            int count = await _uow.Projects.CountAsync(p => p.UserId == project.UserId && !p.IsLocked && !p.IsDeleted);

            var availableProjectsList = await _uow.Subscriptions.SelectWhereAsync(
                selector: s => s.Plan.MaxProjects,
                criteria: s => s.UserId == project.UserId && s.Status == Subscription.Statuses.Active && s.EndDate > DateTime.UtcNow,
                includes: q => q.Include(s => s.Plan)
            );
            int availableProjects = availableProjectsList.FirstOrDefault();

            if (count >= availableProjects)
            {
                return Result<bool>.Failure($"You have reached the maximum number of unlocked projects allowed for your current plan ({availableProjects}). Please upgrade your plan to unlock more projects.", StatusCodes.Status403Forbidden);
            }

            project.IsLocked = false;
            _uow.Projects.Update(project);
            await _uow.CompleteAsync();

            return Result<bool>.Success(true);
        }
       
        private static ProjectDetailsDto MapToDetailsDto(Project project)
        {
            return new ProjectDetailsDto
            {
                Id = project.Id,
                Name = project.Name,
                PublicApiKey = project.PublicApiKey,
                PrivateApiKey = project.PrivateApiKey,
                RestrictedDomains = project.RestrictedDomains,
                ProjectAccessMode = project.ProjectAccessMode,
                IsActive = project.IsActive,
                IsLocked = project.IsLocked,
                CreatedAt = project.CreatedAt
            };
        }
    }
}