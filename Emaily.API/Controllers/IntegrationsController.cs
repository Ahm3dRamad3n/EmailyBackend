using Emaily.API.Extensions;
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
    [Route("api/integrations")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting("perUser")]
    public class IntegrationsController(IIntegrationService integrationService) : ControllerBase
    {
        private readonly IIntegrationService _integrationService = integrationService;

        [HttpPut("{intId}")]
        public async Task<IActionResult> UpdateIntegration(string intId, [FromBody] UpdateIntegrationDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _integrationService.UpdateIntegrationAsync(User.GetUserId(), intId, dto);
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(result.Data);
        }

        [HttpPut("{intId}/status")]
        public async Task<IActionResult> ToggleIntegrationStatus(string intId, [FromBody] UpdateStatusDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var updated = await _integrationService.ToggleStatusAsync(User.GetUserId(), intId, dto.IsActive);
            if (!updated) return NotFound(new { message = "Integration not found or access denied." });
            return Ok(new { message = "Integration status updated successfully." });
        }

        [HttpDelete("{intId}")]
        public async Task<IActionResult> RemoveIntegration(string intId)
        {
            var deleted = await _integrationService.RemoveIntegrationAsync(User.GetUserId(), intId);
            if (!deleted) return NotFound(new { message = "Integration not found or access denied." });
            return Ok(new { message = "Integration removed successfully." });
        }
    }
}