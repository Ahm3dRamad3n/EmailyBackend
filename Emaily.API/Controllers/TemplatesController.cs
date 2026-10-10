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
    [Route("api/templates/{templateId}")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting("perUser")]
    [ServiceFilter(typeof(CheckTemplateOwnershipFilter))]
    public class TemplatesController(ITemplateService templateService) : ControllerBase
    {
        private readonly ITemplateService _templateService = templateService;

        [HttpGet]
        public async Task<IActionResult> GetTemplateById(string templateId)
        {
            var template = await _templateService.GetTemplateDetailsAsync(templateId);
            if (!template.IsSuccess) return StatusCode(template.ErrorCode, new { success = false, message = template.ErrorMessage });
            return Ok(template.Data);
        }

        [HttpPut]
        [RequestSizeLimit(3 * 1024 * 1024)] // 3 MB
        public async Task<IActionResult> UpdateTemplate(string templateId, [FromBody] UpdateTemplateDto dto)
        {
            var result = await _templateService.UpdateTemplateAsync(templateId, dto);
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(result.Data);
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteTemplate(string templateId)
        {
            var deleted = await _templateService.DeleteTemplateAsync(templateId);
            if (!deleted) return NotFound(new { message = "Template not found or access denied." });
            return Ok(new { message = "Template deleted successfully." });
        }

        [HttpPut("status")]
        public async Task<IActionResult> ToggleStatus(string templateId, [FromBody] Emaily.BLL.DTOs.UpdateStatusDto dto)
        {
            var success = await _templateService.ToggleStatusAsync(templateId, dto.IsActive);
            if (!success) return NotFound(new { message = "Template not found or access denied." });
            return Ok(new { message = "Template status updated successfully." });
        }

        [HttpPut("unlock")]
        [EnableRateLimiting("StrictUserCreationPolicy")]
        public async Task<IActionResult> UnlockTemplate(string templateId)
        {
            var result = await _templateService.UnlockTemplateAsync(templateId);
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(new { message = "Template unlocked successfully."});
        }
    }
}