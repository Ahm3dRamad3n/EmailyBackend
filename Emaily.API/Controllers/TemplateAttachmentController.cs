using Emaily.API.Extensions;
using Emaily.API.Filters;
using Emaily.BLL.DTOs.Template;
using Emaily.BLL.Interfaces;
using Emaily.DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Emaily.API.Controllers
{
    [Route("api/attachments")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting("perUser")]
    public class TemplateAttachmentController(ITemplateService templateService) : ControllerBase
    {
        private readonly ITemplateService _templateService = templateService;

        [HttpPost("upload/{templateId}")]
        [EnableRateLimiting("StrictUserCreationPolicy")]
        [RequestSizeLimit(5 * 1024 * 1024)] // 5 MB
        [ServiceFilter(typeof(CheckTemplateOwnershipFilter))]
        public async Task<IActionResult> UploadAttachment(string templateId, IFormFile file)
        {
            var result = await _templateService.AddAttachmentAsync(User.GetUserId(), templateId, file);
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(result.Data);
        }

        [HttpDelete("{attachmentId}")]
        [ServiceFilter(typeof(CheckAttachmentOwnershipFilter))]
        public async Task<IActionResult> DeleteAttachment(string attachmentId)
        {
            var deleted = await _templateService.DeleteAttachmentAsync(attachmentId);
            if (!deleted) return NotFound(new { message = "Attachment not found or access denied." });
            return Ok(new { message = "Attachment deleted successfully." });
        }
    }
}