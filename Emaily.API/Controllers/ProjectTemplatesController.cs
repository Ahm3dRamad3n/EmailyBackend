using Emaily.API.Extensions;
using Emaily.BLL.DTOs.Template;
using Emaily.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Emaily.API.Controllers
{
    [Route("api/projects/{id}/templates")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting("perUser")]
    public class ProjectTemplatesController(ITemplateService templateService) : ControllerBase
    {
        private readonly ITemplateService _templateService = templateService;

        [HttpGet]
        public async Task<IActionResult> GetTemplates(string id)
        {
            var result = await _templateService.GetProjectTemplatesAsync(User.GetUserId(), id);
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(result.Data);
        }

        [HttpPost]
        [RequestSizeLimit(3 * 1024 * 1024)] // 3 MB
        [EnableRateLimiting("StrictUserCreationPolicy")]
        public async Task<IActionResult> AddTemplate(string id, [FromBody] CreateTemplateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _templateService.CreateTemplateAsync(User.GetUserId(), id, dto);
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(result.Data);
        }
    }
}