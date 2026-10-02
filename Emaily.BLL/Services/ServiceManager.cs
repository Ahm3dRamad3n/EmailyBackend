using Emaily.BLL.DTOs;
using Emaily.BLL.DTOs.Service;
using Emaily.BLL.Helpers.Interfaces;
using Emaily.BLL.Interfaces;
using Emaily.DAL.Entities;
using Emaily.DAL.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Emaily.BLL.Services
{
    public class ServiceManager(IUnitOfWork uow, IEncryptionHelper encryptionHelper) : IServiceManager
    {
        private readonly IUnitOfWork _uow = uow;
        private readonly IEncryptionHelper _encryptionHelper = encryptionHelper;

        public async Task<Result<IEnumerable<ServiceDto>>> GetAllServicesAsync(Guid userId)
        {
            var services = await _uow.Services.FindAllAsync(
                s => s.UserId == userId && !s.IsDeleted,
                includes: s => s.Include(s => s.ServiceApiKey)
                     .Include(s => s.ServiceOauth)
                     .Include(s => s.ServiceAppPassword)
            );

            return Result < IEnumerable < ServiceDto > >.Success(services.Select(s => new ServiceDto
            {
                Id = s.Id,
                ProviderType = s.ProviderType,
                FromEmail = s.FromEmail,
                FromName = s.FromName,

                ServiceApiKey = s.ServiceApiKey != null ? new ServiceApiKeyDto
                {
                    ProviderName = s.ServiceApiKey.ProviderName,
                    SecretApiKey = _encryptionHelper.Decrypt(s.ServiceApiKey.SecretApiKey)
                } : null,

                ServiceOauth = s.ServiceOauth != null ? new ServiceOauthDto
                {
                    OauthProvider = s.ServiceOauth.OauthProvider,
                    TokenExpiry = s.ServiceOauth.TokenExpiry,
                    IsOAuthConnected = !string.IsNullOrEmpty(s.ServiceOauth.AccessToken) && !string.IsNullOrEmpty(s.ServiceOauth.RefreshToken)
                } : null,

                ServiceAppPassword = s.ServiceAppPassword != null ? new ServiceAppPasswordDto
                {
                    SmtpHost = s.ServiceAppPassword.SmtpHost,
                    SmtpPort = s.ServiceAppPassword.SmtpPort,
                    Username = s.ServiceAppPassword.Username,
                    Password = _encryptionHelper.Decrypt(s.ServiceAppPassword.EncryptedPassword)
                } : null,

                IsActive = s.IsActive,
                IsLocked = s.IsLocked
            }));
        }

        public async Task<Result<IEnumerable<ServiceDto>>> GetProjectServicesAsync(Guid userId, string projectId)
        {
            // التحقق من ملكية المشروع أولاً
            var project = await _uow.Projects.FindAsync(p => p.Id == projectId && p.UserId == userId && !p.IsDeleted);
            if (project == null) return Result<IEnumerable<ServiceDto>>.Failure("Project not found or access denied.", StatusCodes.Status403Forbidden);

            var servicesIds = await _uow.ProjectServices.SelectWhereAsync(selector: ps => ps.ServiceId, criteria: ps => ps.ProjectId == projectId);

            var services = await _uow.Services.FindAllAsync(s => servicesIds.Contains(s.Id) && !s.IsDeleted);

            return Result<IEnumerable<ServiceDto>>.Success(
            services.Select(s => new ServiceDto
            {
                Id = s.Id,
                ProviderType = s.ProviderType,
                FromEmail = s.FromEmail,
                FromName = s.FromName,
                IsActive = s.IsActive,
                IsLocked = s.IsLocked,
            }));
        }

        public async Task<Result<ServiceDto>> AddServiceAsync(Guid userId, CreateServiceDto dto)
        {
            var subscriptions = await _uow.Subscriptions.FindAllAsync(s => s.UserId == userId && s.Status == Subscription.Statuses.Active && s.EndDate > DateTime.UtcNow);
            var activeSubscription = subscriptions.OrderByDescending(s => s.CreatedAt).FirstOrDefault();
            if (activeSubscription == null) return Result<ServiceDto>.Failure("No active subscription found. Please subscribe to a plan to add a service.", StatusCodes.Status403Forbidden);

            var plan = await _uow.Plans.FindAsync(p => p.Id == activeSubscription.PlanId);
            if (plan == null) return Result<ServiceDto>.Failure("Plan associated with the active subscription not found.", StatusCodes.Status404NotFound);

            var currentServicesCount = await _uow.Services.CountAsync(ps => ps.UserId == userId && !ps.IsDeleted);
            if (currentServicesCount >= plan.MaxServices)
                return Result<ServiceDto>.Failure($"Service limit reached for your current plan ({plan.Name}). Please upgrade your plan to add more services.", StatusCodes.Status403Forbidden);

            var service = new Service
            {
                Id = "s_" + Guid.NewGuid().ToString("N"),
                UserId = userId,
                ProviderType = dto.ProviderType,
                FromEmail = dto.FromEmail,
                FromName = dto.FromName,
                IsActive = true,
                IsDeleted = false
            };

            if (dto.ProviderType == Service.ProviderTypes.AppPassword)
            {
                service.ServiceAppPassword = new ServiceAppPassword
                {
                    SmtpHost = dto.ServiceAppPassword!.SmtpHost,
                    SmtpPort = dto.ServiceAppPassword.SmtpPort,
                    Username = dto.ServiceAppPassword.Username,
                    EncryptedPassword = _encryptionHelper.Encrypt(dto.ServiceAppPassword.Password)
                };
            }
            else if (dto.ProviderType == Service.ProviderTypes.ApiKey)
            {
                service.ServiceApiKey = new ServiceApiKey
                {
                    ProviderName = dto.ServiceApiKey!.ProviderName,
                    SecretApiKey = _encryptionHelper.Encrypt(dto.ServiceApiKey.SecretApiKey)
                };
            }
            else if (dto.ProviderType == Service.ProviderTypes.OAuth)
            {
                service.ServiceOauth = new ServiceOauth
                {
                    OauthProvider = dto.ServiceOauth!.OauthProvider,
                    AccessToken = dto.ServiceOauth.AccessToken,
                    RefreshToken = dto.ServiceOauth.RefreshToken,
                    TokenExpiry = dto.ServiceOauth.TokenExpiry
                };
            }

            await _uow.Services.AddAsync(service);
            await _uow.CompleteAsync();

            return Result<ServiceDto>.Success(
            new ServiceDto
            {
                Id = service.Id,
                UserId = service.UserId.ToString(),
                ProviderType = service.ProviderType,
                FromEmail = service.FromEmail,
                FromName = service.FromName,
                IsActive = service.IsActive,
                IsLocked = service.IsLocked,

                ServiceApiKey = service.ServiceApiKey != null ? new ServiceApiKeyDto
                {
                    ProviderName = service.ServiceApiKey.ProviderName
                } : null,

                ServiceOauth = service.ServiceOauth != null ? new ServiceOauthDto
                {
                    OauthProvider = service.ServiceOauth.OauthProvider,
                    TokenExpiry = service.ServiceOauth.TokenExpiry,
                    IsOAuthConnected = !string.IsNullOrEmpty(service.ServiceOauth.AccessToken) && !string.IsNullOrEmpty(service.ServiceOauth.RefreshToken)
                } : null,

                ServiceAppPassword = service.ServiceAppPassword != null ? new ServiceAppPasswordDto
                {
                    SmtpHost = service.ServiceAppPassword.SmtpHost,
                    SmtpPort = service.ServiceAppPassword.SmtpPort,
                    Username = service.ServiceAppPassword.Username
                } : null
            });
        }

        public async Task<Result<ServiceDto>> UpdateServiceAsync(Guid userId, string serviceId, UpdateServiceDto dto)
        {
            var service = await _uow.Services.FindAsync(s => s.Id == serviceId && s.UserId == userId && !s.IsDeleted,
                includes: s => s.Include(s => s.ServiceApiKey)
                     .Include(s => s.ServiceOauth)
                     .Include(s => s.ServiceAppPassword));
            if (service == null) return Result<ServiceDto>.Failure("Service not found or access denied.", StatusCodes.Status404NotFound);

            if (service.IsLocked)
                return Result<ServiceDto>.Failure("This service is locked due to plan downgrade. Please upgrade your plan to unlock it.", StatusCodes.Status403Forbidden);

            // تحديث البيانات 
            if (service.ProviderType == Service.ProviderTypes.AppPassword && service.ServiceAppPassword != null)
            {
                service.ServiceAppPassword.SmtpHost = dto.ServiceAppPassword?.SmtpHost ?? service.ServiceAppPassword.SmtpHost;
                service.ServiceAppPassword.SmtpPort = dto.ServiceAppPassword?.SmtpPort ?? service.ServiceAppPassword.SmtpPort;
                service.ServiceAppPassword.Username = dto.ServiceAppPassword?.Username ?? service.ServiceAppPassword.Username;
                if (!string.IsNullOrEmpty(dto.ServiceAppPassword?.Password))
                    service.ServiceAppPassword.EncryptedPassword = _encryptionHelper.Encrypt(dto.ServiceAppPassword.Password);
            }
            else if (service.ProviderType == Service.ProviderTypes.ApiKey && service.ServiceApiKey != null)
            {
                if (!string.IsNullOrEmpty(dto.ServiceApiKey?.SecretApiKey))
                    service.ServiceApiKey.SecretApiKey = _encryptionHelper.Encrypt(dto.ServiceApiKey.SecretApiKey);
            }
            else if (service.ProviderType == Service.ProviderTypes.OAuth && service.ServiceOauth != null)
            {
                service.ServiceOauth.AccessToken = string.IsNullOrEmpty(dto.ServiceOauth?.AccessToken) ? service.ServiceOauth.AccessToken : dto.ServiceOauth.AccessToken;
                service.ServiceOauth.RefreshToken = string.IsNullOrEmpty(dto.ServiceOauth?.RefreshToken) ? service.ServiceOauth.RefreshToken : dto.ServiceOauth.RefreshToken;
                service.ServiceOauth.TokenExpiry = dto.ServiceOauth?.TokenExpiry ?? service.ServiceOauth.TokenExpiry;
            }

            service.FromEmail = dto.FromEmail ?? service.FromEmail;
            service.FromName = dto.FromName ?? service.FromName;

            _uow.Services.Update(service);
            await _uow.CompleteAsync();

            return Result<ServiceDto>.Success(
            new ServiceDto
            {
                Id = service.Id,
                UserId = service.UserId.ToString(),
                ProviderType = service.ProviderType,
                FromEmail = service.FromEmail,
                FromName = service.FromName,
                IsActive = service.IsActive,

                ServiceApiKey = service.ServiceApiKey != null ? new ServiceApiKeyDto
                {
                    ProviderName = service.ServiceApiKey.ProviderName
                } : null,

                ServiceOauth = service.ServiceOauth != null ? new ServiceOauthDto
                {
                    OauthProvider = service.ServiceOauth.OauthProvider,
                    TokenExpiry = service.ServiceOauth.TokenExpiry,
                    IsOAuthConnected = !string.IsNullOrEmpty(service.ServiceOauth.AccessToken) && !string.IsNullOrEmpty(service.ServiceOauth.RefreshToken)
                } : null,

                ServiceAppPassword = service.ServiceAppPassword != null ? new ServiceAppPasswordDto
                {
                    SmtpHost = service.ServiceAppPassword.SmtpHost,
                    SmtpPort = service.ServiceAppPassword.SmtpPort,
                    Username = service.ServiceAppPassword.Username
                } : null
            });
        }

        public async Task<bool> DeleteServiceAsync(Guid userId, string serviceId)
        {
            var service = await _uow.Services.FindAsync(s => s.Id == serviceId && !s.IsDeleted, includes: q => q.Include(s => s.ProjectServices));
            if (service == null) return false;

            service.IsDeleted = true;
            service.IsActive = false;

            foreach (var projectService in service.ProjectServices)
            {
                _uow.ProjectServices.Delete(projectService);
            }

            _uow.Services.Update(service);
            await _uow.CompleteAsync();

            return true;
        }
       
        public async Task<bool> ToggleStatusAsync(Guid userId, string serviceId, bool isActive)
        {
            var service = await _uow.Services.FindAsync(s => s.Id == serviceId && s.UserId == userId && !s.IsDeleted);
            if (service == null) return false;

            service.IsActive = isActive;
            _uow.Services.Update(service);
            await _uow.CompleteAsync();
            return true;
        }

        public async Task<Result<bool>> LinkServiceToProjectAsync(Guid userId, string serviceId, string projectId)
        {
            var service = await _uow.Services.FindAsync(s => s.Id == serviceId && s.UserId == userId && !s.IsDeleted);
            if (service == null) return Result<bool>.Failure("Service not found.", StatusCodes.Status404NotFound);

            var project = await _uow.Projects.FindAsync(p => p.Id == projectId && p.UserId == userId && !p.IsDeleted);
            if (project == null) return Result<bool>.Failure("Project not found or access denied.", StatusCodes.Status403Forbidden);

            var existingLink = await _uow.ProjectServices.FindAsync(ps => ps.ServiceId == serviceId && ps.ProjectId == projectId);
            if (existingLink != null) return Result<bool>.Failure("Service is already linked to the project.", StatusCodes.Status400BadRequest);

            var projectService = new ProjectService
            {
                ProjectId = projectId,
                ServiceId = serviceId,
            };

            await _uow.ProjectServices.AddAsync(projectService);
            await _uow.CompleteAsync();
            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> UnlinkServiceFromProjectAsync(Guid userId, string serviceId, string projectId)
        {
            var service = await _uow.Services.FindAsync(s => s.Id == serviceId && s.UserId == userId && !s.IsDeleted);
            if (service == null) return Result<bool>.Failure("Service not found.", StatusCodes.Status404NotFound);

            var project = await _uow.Projects.FindAsync(p => p.Id == projectId && p.UserId == userId && !p.IsDeleted);
            if (project == null) return Result<bool>.Failure("Project not found or access denied.", StatusCodes.Status403Forbidden);

            var existingLink = await _uow.ProjectServices.FindAsync(ps => ps.ServiceId == serviceId && ps.ProjectId == projectId);
            if (existingLink == null) return Result<bool>.Failure("Service is not linked to the project.", StatusCodes.Status400BadRequest);

            _uow.ProjectServices.Delete(existingLink);
            await _uow.CompleteAsync();
            return Result<bool>.Success(true);
        }
        public async Task<Result<bool>> UnlockServiceAsync(Guid userId, string serviceId)
        {
            var service = await _uow.Services.FindAsync(s => s.Id == serviceId && s.UserId == userId && !s.IsDeleted);

            if (service == null)
                return Result<bool>.Failure("Service not found or access denied.", StatusCodes.Status404NotFound);

            if (!service.IsLocked)
                return Result<bool>.Success(true);

            int count = await _uow.Services.CountAsync(s => s.UserId == userId && !s.IsLocked && !s.IsDeleted);

            var availableServicesList = await _uow.Subscriptions.SelectWhereAsync(
                selector: sub => sub.Plan.MaxServices, 
                criteria: sub => sub.UserId == userId && sub.Status == Subscription.Statuses.Active && sub.EndDate > DateTime.UtcNow,
                includes: q => q.Include(sub => sub.Plan)
            );
            int availableServices = availableServicesList.FirstOrDefault();

            if (count >= availableServices)
            {
                return Result<bool>.Failure($"You have reached the maximum number of unlocked services allowed for your current plan ({availableServices}). Please upgrade your plan to unlock more services.", StatusCodes.Status403Forbidden);
            }

            service.IsLocked = false;
            _uow.Services.Update(service);
            await _uow.CompleteAsync();

            return Result<bool>.Success(true);
        }
    }
}