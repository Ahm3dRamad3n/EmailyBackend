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
        Task<Result<IEnumerable<TemplateDto>>> GetProjectTemplatesAsync(string projectId);
        Task<Result<TemplateDetailsDto>> GetTemplateDetailsAsync(string templateId);
        Task<Result<TemplateDetailsDto>> CreateTemplateAsync(string projectId, CreateTemplateDto dto);
        Task<Result<TemplateDetailsDto>> UpdateTemplateAsync(string templateId, UpdateTemplateDto dto);
        Task<Result<TemplateAttachmentDto>> AddAttachmentAsync(Guid userId, string templateId, IFormFile file);
        Task<bool> DeleteAttachmentAsync(string attachmentId);
        Task<bool> DeleteTemplateAsync(string templateId);
        Task<bool> ToggleStatusAsync(string templateId, bool isActive);
        Task<Result<bool>> UnlockTemplateAsync(string templateId);
    }
}