using Emaily.API.Extensions;
using Emaily.API.Filters;
using Emaily.BLL.DTOs;
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
    [Route("api/integrations/{integrationId}")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting("perUser")]
    [ServiceFilter(typeof(CheckIntegrationOwnershipFilter))]
    public class IntegrationsController(IIntegrationService integrationService) : ControllerBase
    {
        private readonly IIntegrationService _integrationService = integrationService;

        [HttpPut]
        public async Task<IActionResult> UpdateIntegration(string integrationId, [FromBody] UpdateIntegrationDto dto)
        {
            var result = await _integrationService.UpdateIntegrationAsync(User.GetUserId(), integrationId, dto);
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(result.Data);
        }

        [HttpPut("status")]
        public async Task<IActionResult> ToggleIntegrationStatus(string integrationId, [FromBody] UpdateStatusDto dto)
        {
            var updated = await _integrationService.ToggleStatusAsync(integrationId, dto.IsActive);
            if (!updated) return NotFound(new { message = "Integration not found or access denied." });
            return Ok(new { message = "Integration status updated successfully." });
        }

        [HttpDelete]
        public async Task<IActionResult> RemoveIntegration(string integrationId)
        {
            var deleted = await _integrationService.RemoveIntegrationAsync(integrationId);
            if (!deleted) return NotFound(new { message = "Integration not found or access denied." });
            return Ok(new { message = "Integration removed successfully." });
        }
    }
}