using Emaily.BLL.DTOs;
using Emaily.BLL.DTOs.Template;
using Emaily.BLL.Helpers;
using Emaily.BLL.Helpers.Interfaces;
using Emaily.BLL.Helpers.Services;
using Emaily.BLL.Interfaces;
using Emaily.DAL.Entities;
using Emaily.DAL.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Emaily.BLL.Services
{
    public class TemplateService(IUnitOfWork uow, IAttachmentManager attachmentManager) : ITemplateService
    {
        private readonly IUnitOfWork _uow = uow;
        private readonly IAttachmentManager _am = attachmentManager;

        public async Task<Result<IEnumerable<TemplateDto>>> GetProjectTemplatesAsync(Guid userId, string projectId)
        {
            var project = await _uow.Projects.FindAsync(p => p.Id == projectId && p.UserId == userId && !p.IsDeleted);
            if (project == null) return Result<IEnumerable<TemplateDto>>.Failure("Project not found or access denied.", StatusCodes.Status403Forbidden);

            var templates = await _uow.Templates.SelectWhereAsync(selector: t => new TemplateDto
            {
                Id = t.Id,
                ProjectId = t.ProjectId,
                Name = t.Name,
                Subject = t.Subject,
                IsActive = t.IsActive,
                IsLocked = t.IsLocked,
            },
            criteria: t => t.ProjectId == projectId && !t.IsDeleted);

            return Result<IEnumerable<TemplateDto>>.Success(templates);
        }

        public async Task<Result<TemplateDetailsDto>> GetTemplateDetailsAsync(Guid userId, string templateId)
        {
            var template = await _uow.Templates.FindAsync(t => t.Id == templateId && !t.IsDeleted, includes: t => t.Include(x => x.TemplateAttachments));
            if (template == null) return Result<TemplateDetailsDto>.Failure("Template not found.", StatusCodes.Status404NotFound);

            var project = await _uow.Projects.FindAsync(p => p.Id == template.ProjectId && p.UserId == userId && !p.IsDeleted);
            if (project == null) return Result<TemplateDetailsDto>.Failure("Access denied.", StatusCodes.Status403Forbidden);

            return Result<TemplateDetailsDto>.Success(MapToDetailsDto(template));
        }

        public async Task<Result<TemplateDetailsDto>> CreateTemplateAsync(Guid userId, string projectId, CreateTemplateDto dto)
        {
            var project = await _uow.Projects.FindAsync(p => p.Id == projectId && p.UserId == userId && !p.IsDeleted);
            if (project == null) return Result<TemplateDetailsDto>.Failure("Project not found or access denied.", StatusCodes.Status403Forbidden);

            if (project.IsLocked)
                return Result<TemplateDetailsDto>.Failure("This project is locked due to plan limits. Please upgrade your plan to unlock it.", StatusCodes.Status403Forbidden);

            var subscriptions = await _uow.Subscriptions.FindAllAsync(s => s.UserId == userId && s.Status == Subscription.Statuses.Active && s.EndDate > DateTime.UtcNow);
            var activeSubscription = subscriptions.OrderByDescending(s => s.CreatedAt).FirstOrDefault();
            if (activeSubscription == null)
                return Result<TemplateDetailsDto>.Failure("No active subscription found.", StatusCodes.Status403Forbidden);

            var plan = await _uow.Plans.FindAsync(p => p.Id == activeSubscription.PlanId);
            if (plan == null)
                return Result<TemplateDetailsDto>.Failure("Plan not found.", StatusCodes.Status403Forbidden);

            var currentTemplates = await _uow.Templates.FindAllAsync(t => t.ProjectId == projectId && !t.IsDeleted);
            if (currentTemplates.Count() >= plan.MaxTemplates)
                return Result<TemplateDetailsDto>.Failure($"You have reached the max templates limit ({plan.MaxTemplates}) for this project.", StatusCodes.Status403Forbidden);

            dto.ServiceId = await _uow.Services.CountAsync(s => s.Id == dto.ServiceId && !s.IsDeleted) > 0 ? dto.ServiceId : null;

            if (!HtmlSecurityValidator.IsHtmlSafe(dto.ContentHtml))
                return Result<TemplateDetailsDto>.Failure("The HTML content contains unsafe elements or attributes.", StatusCodes.Status403Forbidden);

            var template = new Template
            {
                Id = "t_" + Guid.NewGuid().ToString("N"),
                ProjectId = projectId,
                ServiceId = dto.ServiceId,
                Name = dto.Name,
                Subject = dto.Subject,
                ContentHtml = dto.ContentHtml,
                DoSaveInHistory = dto.DoSaveInHistory,
                EnableRecaptchaV2 = dto.EnableRecaptchaV2,
                RecaptchaSecretKey = dto.EnableRecaptchaV2 == true ? dto.RecaptchaSecretKey : null,
                EnableAppCheck = dto.EnableAppCheck,
                Issuer = dto.EnableAppCheck == true ? dto.Issuer : null,
                AppCheckSecret = dto.EnableAppCheck == true ? dto.AppCheckSecret : null,
                ToEmail = dto.ToEmail,
                ReplyTo = dto.ReplyTo,
                Bcc = dto.Bcc,
                Cc = dto.Cc,
                EnableAutoReply = dto.EnableAutoReply,
                AutoReplyTemplateId = dto.EnableAutoReply == true ? dto.AutoReplyTemplateId : null,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                IsDeleted = false
            };

            await _uow.Templates.AddAsync(template);
            await _uow.CompleteAsync();

            return Result<TemplateDetailsDto>.Success(MapToDetailsDto(template));
        }

        public async Task<Result<TemplateDetailsDto>> UpdateTemplateAsync(Guid userId, string templateId, UpdateTemplateDto dto)
        {
            var template = await _uow.Templates.FindAsync(t => t.Id == templateId && !t.IsDeleted);
            if (template == null) return Result<TemplateDetailsDto>.Failure("Template not found.", StatusCodes.Status404NotFound);

            if (template.IsLocked)
                return Result<TemplateDetailsDto>.Failure("This template is locked due to plan limits. Please upgrade your plan to unlock it.", StatusCodes.Status403Forbidden);

            var project = await _uow.Projects.FindAsync(p => p.Id == template.ProjectId && p.UserId == userId && !p.IsDeleted);
                if (project == null) return Result<TemplateDetailsDto>.Failure("Access denied.", StatusCodes.Status403Forbidden);

            dto.ServiceId = await _uow.Services.CountAsync(s => s.Id == dto.ServiceId && !s.IsDeleted) > 0 ? dto.ServiceId : null;

            if (!HtmlSecurityValidator.IsHtmlSafe(dto.ContentHtml))
                return Result<TemplateDetailsDto>.Failure("The HTML content contains unsafe elements or attributes.", StatusCodes.Status403Forbidden);

            template.ServiceId = dto.ServiceId;
            template.Name = dto.Name;
            template.Subject = dto.Subject;
            template.ContentHtml = dto.ContentHtml;
            template.DoSaveInHistory = dto.DoSaveInHistory;
            template.EnableRecaptchaV2 = dto.EnableRecaptchaV2;
            template.RecaptchaSecretKey = dto.EnableRecaptchaV2 == true ? dto.RecaptchaSecretKey : null;
            template.EnableAppCheck = dto.EnableAppCheck;
            template.Issuer = dto.EnableAppCheck == true ? dto.Issuer : null; 
            template.AppCheckSecret = dto.EnableAppCheck == true ? dto.AppCheckSecret : null;
            template.ToEmail = dto.ToEmail;
            template.ToName = dto.ToName;
            template.ReplyTo = dto.ReplyTo;
            template.Bcc = dto.Bcc;
            template.Cc = dto.Cc;
            template.EnableAutoReply = dto.EnableAutoReply;
            template.AutoReplyTemplateId = dto.EnableAutoReply == true ? dto.AutoReplyTemplateId : null;

            _uow.Templates.Update(template);
            await _uow.CompleteAsync();

            return Result<TemplateDetailsDto>.Success(MapToDetailsDto(template));   
        }

        public async Task<Result<TemplateAttachmentDto>> AddAttachmentAsync(Guid userId, string templateId, IFormFile file)
        {
            var template = await _uow.Templates.FindAsync(t => t.Id == templateId && !t.IsDeleted);
            if (template == null) return Result<TemplateAttachmentDto>.Failure("Template not found.", StatusCodes.Status404NotFound);

            if (template.IsLocked)
                return Result<TemplateAttachmentDto>.Failure("This template is locked due to plan limits. Please upgrade your plan to unlock it.", StatusCodes.Status403Forbidden);

            var project = await _uow.Projects.FindAsync(p => p.Id == template.ProjectId && p.UserId == userId && !p.IsDeleted);
            if (project == null) return Result<TemplateAttachmentDto>.Failure("Access denied.", StatusCodes.Status403Forbidden);

            // التحقق من باقة المستخدم لعدد المرفقات
            var subscriptions = await _uow.Subscriptions.FindAllAsync(s => s.UserId == userId && s.Status == Subscription.Statuses.Active && s.EndDate > DateTime.UtcNow);
            var activeSub = subscriptions.OrderByDescending(s => s.CreatedAt).FirstOrDefault();
            if (activeSub == null)
                return Result<TemplateAttachmentDto>.Failure("No active subscription found.", StatusCodes.Status403Forbidden);
            var plan = await _uow.Plans.FindAsync(p => p.Id == activeSub.PlanId);
            if (plan == null)
                return Result<TemplateAttachmentDto>.Failure("Plan not found.", StatusCodes.Status403Forbidden);

            var currentAttachments = await _uow.TemplateAttachments.FindAllAsync(a => a.TemplateId == templateId);
            if (currentAttachments.Count() >= plan.MaxAttachmentsPerTemplate)
                return Result<TemplateAttachmentDto>.Failure($"You have reached the max attachments limit ({plan.MaxAttachmentsPerTemplate}) for this template.", StatusCodes.Status403Forbidden);
           
            string? url = _am.Add(file, AttachmentManager.AttachmentSource.Template);
            if (url == null)
                return Result<TemplateAttachmentDto>.Failure("Failed to upload the attachment. Please ensure the file is valid and try again.", StatusCodes.Status400BadRequest);

            var attachment = new TemplateAttachment
            {
                Id = Guid.NewGuid(),
                TemplateId = templateId,
                FileName = file.FileName,
                FileUrl = url,
                FileSizeInBytes = file.Length
            };

            await _uow.TemplateAttachments.AddAsync(attachment);
            await _uow.CompleteAsync();

            return Result<TemplateAttachmentDto>.Success(
            new TemplateAttachmentDto
            {
                Id = attachment.Id.ToString(),
                FileName = attachment.FileName,
                FileUrl = attachment.FileUrl,
                FileSizeInBytes = attachment.FileSizeInBytes
            });
        }

        public async Task<bool> ToggleStatusAsync(Guid userId, string templateId, bool isActive)
        {
            var template = await _uow.Templates.FindAsync(t => t.Id == templateId && !t.IsDeleted);
            if (template == null) return false;

            var project = await _uow.Projects.FindAsync(p => p.Id == template.ProjectId && p.UserId == userId && !p.IsDeleted);
            if (project == null) return false;

            template.IsActive = isActive;
            _uow.Templates.Update(template);
            await _uow.CompleteAsync();
            return true;
        }

        public async Task<bool> DeleteTemplateAsync(Guid userId, string templateId)
        {
            var template = await _uow.Templates.FindAsync(t => t.Id == templateId && !t.IsDeleted, includes: t => t.Include(x => x.TemplateAttachments));
            if (template == null) return false;

            var project = await _uow.Projects.FindAsync(p => p.Id == template.ProjectId && p.UserId == userId && !p.IsDeleted);
            if (project == null) return false;

            template.IsDeleted = true;
            template.IsActive = false;

            foreach (var attachment in template.TemplateAttachments)
            {
                // حذف الملف الفيزيائي من السيرفر
                _am.Delete(attachment.FileUrl, AttachmentManager.AttachmentSource.Template);
                _uow.TemplateAttachments.Delete(attachment);
            }

            _uow.Templates.Update(template);
            await _uow.CompleteAsync();
            return true;
        }

        public async Task<bool> DeleteAttachmentAsync(Guid userId, string templateId, string attachmentId)
        {
            var template = await _uow.Templates.FindAsync(t => t.Id == templateId && !t.IsDeleted);
            if (template == null) return false;

            var project = await _uow.Projects.FindAsync(p => p.Id == template.ProjectId && p.UserId == userId && !p.IsDeleted);
            if (project == null) return false;

            if (!Guid.TryParse(attachmentId, out var parsedId)) return false;

            var attachment = await _uow.TemplateAttachments.FindAsync(a => a.Id == parsedId && a.TemplateId == templateId);
            if (attachment == null) return false;

            // حذف الملف الفيزيائي من السيرفر
            _am.Delete(attachment.FileUrl, AttachmentManager.AttachmentSource.Template);

            _uow.TemplateAttachments.Delete(attachment);
            await _uow.CompleteAsync();
            return true;
        }

        public async Task<Result<bool>> UnlockTemplateAsync(Guid userId, string templateId)
        {
            var template = await _uow.Templates.FindAsync(t => t.Id == templateId && !t.IsDeleted);
            if (template == null)
                return Result<bool>.Failure("Template not found.", StatusCodes.Status404NotFound);

            if (!template.IsLocked)
                return Result<bool>.Success(true);

            var project = await _uow.Projects.FindAsync(p => p.Id == template.ProjectId && p.UserId == userId && !p.IsDeleted && !p.IsLocked);
            if (project == null)
                return Result<bool>.Failure("Access denied or project is locked.", StatusCodes.Status403Forbidden);

            int count = await _uow.Templates.CountAsync(t => !t.IsLocked && !t.IsDeleted && t.Project.UserId == userId);

            var availableTemplatesList = await _uow.Subscriptions.SelectWhereAsync(
                selector: s => s.Plan.MaxTemplates, 
                criteria: s => s.UserId == userId && s.Status == Subscription.Statuses.Active && s.EndDate > DateTime.UtcNow,
                includes: q => q.Include(s => s.Plan)
            );
            int availableTemplates = availableTemplatesList.FirstOrDefault();

            if (count >= availableTemplates)
            {
                return Result<bool>.Failure($"You have reached the maximum number of unlocked templates allowed for your current plan ({availableTemplates}). Please upgrade your plan to unlock more templates.", StatusCodes.Status403Forbidden);
            }

            template.IsLocked = false;
            _uow.Templates.Update(template);
            await _uow.CompleteAsync();

            return Result<bool>.Success(true);
        }
        private static TemplateDetailsDto MapToDetailsDto(Template t)
        {
            return new TemplateDetailsDto
            {
                Id = t.Id,
                ProjectId = t.ProjectId,
                ServiceId = t.ServiceId,
                Name = t.Name,
                Subject = t.Subject,
                ContentHtml = t.ContentHtml,
                DoSaveInHistory = t.DoSaveInHistory,
                EnableRecaptchaV2 = t.EnableRecaptchaV2,
                RecaptchaSecretKey = t.RecaptchaSecretKey,
                EnableAppCheck = t.EnableAppCheck,
                Issuer = t.Issuer,
                AppCheckSecret = t.AppCheckSecret,
                ToEmail = t.ToEmail,
                ToName = t.ToName,
                ReplyTo = t.ReplyTo,
                Bcc = t.Bcc,
                Cc = t.Cc,
                EnableAutoReply = t.EnableAutoReply,
                AutoReplyTemplateId = t.AutoReplyTemplateId,
                IsActive = t.IsActive,
                IsLocked = t.IsLocked,
                Attachments = t.TemplateAttachments?.Select(a => new TemplateAttachmentDto
                {
                    Id = a.Id.ToString(),
                    FileName = a.FileName,
                    FileUrl = a.FileUrl,
                    FileSizeInBytes = a.FileSizeInBytes
                }).ToList() ?? []
            };
        }
    }
}