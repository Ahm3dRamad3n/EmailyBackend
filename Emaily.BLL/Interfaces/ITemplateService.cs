using Emaily.BLL.DTOs;
using Emaily.BLL.DTOs.Template;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Emaily.BLL.Interfaces
{
    public interface ITemplateService
    {
        Task<Result<IEnumerable<TemplateDto>>> GetProjectTemplatesAsync(Guid userId, string projectId);
        Task<Result<TemplateDetailsDto>> GetTemplateDetailsAsync(Guid userId, string templateId);
        Task<Result<TemplateDetailsDto>> CreateTemplateAsync(Guid userId, string projectId, CreateTemplateDto dto);
        Task<Result<TemplateDetailsDto>> UpdateTemplateAsync(Guid userId, string templateId, UpdateTemplateDto dto);
        Task<Result<TemplateAttachmentDto>> AddAttachmentAsync(Guid userId, string templateId, IFormFile file);
        Task<bool> DeleteAttachmentAsync(Guid userId, string templateId, string attachmentId);
        Task<bool> DeleteTemplateAsync(Guid userId, string templateId);
        Task<bool> ToggleStatusAsync(Guid userId, string templateId, bool isActive);
        Task<Result<bool>> UnlockTemplateAsync(Guid userId, string templateId);
    }
}