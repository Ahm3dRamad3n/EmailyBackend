using Emaily.API.Extensions;
using Emaily.API.Filters;
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
    [Route("api/projects/{projectId}/integrations")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting("perUser")]
    [ServiceFilter(typeof(CheckProjectOwnershipFilter))]
    public class ProjectIntegrationsController(IIntegrationService integrationService) : ControllerBase
    {
        private readonly IIntegrationService _integrationService = integrationService;

        [HttpGet]
        public async Task<IActionResult> GetIntegrations(string projectId)
        {
            var result = await _integrationService.GetIntegrationsAsync(projectId);
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(result.Data);
        }

        [HttpPost]
        [EnableRateLimiting("StrictUserCreationPolicy")]
        public async Task<IActionResult> AddIntegration(string projectId, [FromBody] CreateIntegrationDto dto)
        {
            var result = await _integrationService.AddIntegrationAsync(User.GetUserId(), projectId, dto);
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(result.Data);
        }
    }
}