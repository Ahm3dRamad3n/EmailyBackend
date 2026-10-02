using Emaily.API.Extensions;
using Emaily.BLL.DTOs.Integration;
using Emaily.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Emaily.API.Controllers
{
    [Route("api/projects/{id}/integrations")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting("perUser")]
    public class ProjectIntegrationsController(IIntegrationService integrationService) : ControllerBase
    {
        private readonly IIntegrationService _integrationService = integrationService;

        [HttpGet]
        public async Task<IActionResult> GetIntegrations(string id)
        {
            var result = await _integrationService.GetIntegrationsAsync(User.GetUserId(), id);
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(result.Data);
        }

        [HttpPost]
        public async Task<IActionResult> AddIntegration(string id, [FromBody] CreateIntegrationDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _integrationService.AddIntegrationAsync(User.GetUserId(), id, dto);
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(result.Data);
        }
    }
}