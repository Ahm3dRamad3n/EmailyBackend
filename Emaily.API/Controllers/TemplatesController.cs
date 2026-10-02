using Emaily.API.Extensions;
using Emaily.BLL.DTOs.Template;
using Emaily.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Emaily.API.Controllers
{
    [Route("api/templates")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting("perUser")]
    public class TemplatesController(ITemplateService templateService) : ControllerBase
    {
        private readonly ITemplateService _templateService = templateService;

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTemplateById(string id)
        {
            var template = await _templateService.GetTemplateDetailsAsync(User.GetUserId(), id);
            if (!template.IsSuccess) return StatusCode(template.ErrorCode, new { success = false, message = template.ErrorMessage });
            return Ok(template.Data);
        }

        [HttpPut("{id}")]
        [RequestSizeLimit(3 * 1024 * 1024)] // 3 MB
        public async Task<IActionResult> UpdateTemplate(string id, [FromBody] UpdateTemplateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _templateService.UpdateTemplateAsync(User.GetUserId(), id, dto);
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(result.Data);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTemplate(string id)
        {
            var deleted = await _templateService.DeleteTemplateAsync(User.GetUserId(), id);
            if (!deleted) return NotFound(new { message = "Template not found or access denied." });
            return Ok(new { message = "Template deleted successfully." });
        }

        [HttpPost("{id}/attachments")]
        public async Task<IActionResult> UploadAttachment(string id, IFormFile file)
        {
            var result = await _templateService.AddAttachmentAsync(User.GetUserId(), id, file);
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(result.Data);
        }

        [HttpDelete("{id}/attachments/{attachmentId}")]
        public async Task<IActionResult> DeleteAttachment(string id, string attachmentId)
        {
            var deleted = await _templateService.DeleteAttachmentAsync(User.GetUserId(), id, attachmentId);
            if (!deleted) return NotFound(new { message = "Attachment not found or access denied." });
            return Ok(new { message = "Attachment deleted successfully." });
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> ToggleStatus(string id, [FromBody] Emaily.BLL.DTOs.UpdateStatusDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var success = await _templateService.ToggleStatusAsync(User.GetUserId(), id, dto.IsActive);
            if (!success) return NotFound(new { message = "Template not found or access denied." });
            return Ok(new { message = "Template status updated successfully." });
        }

        [HttpPut("{id}/unlock")]
        public async Task<IActionResult> UnlockTemplate(string id)
        {
            var result = await _templateService.UnlockTemplateAsync(User.GetUserId(), id);
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(new { message = "Template unlocked successfully."});
        }
    }
}