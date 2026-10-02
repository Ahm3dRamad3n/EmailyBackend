using Emaily.API.Extensions;
using Emaily.BLL.DTOs.Service;
using Emaily.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Emaily.API.Controllers
{
    [Route("api/projects/{id}/services")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting("perUser")]
    public class ProjectServicesController(IServiceManager serviceManager) : ControllerBase
    {
        private readonly IServiceManager _serviceManager = serviceManager;

        [HttpGet]
        public async Task<IActionResult> GetServices(string id)
        {
            var services = await _serviceManager.GetProjectServicesAsync(User.GetUserId(), id);
            if (!services.IsSuccess) return StatusCode(services.ErrorCode, new { success = false, message = services.ErrorMessage });
            return Ok(services.Data);
        }

        [HttpPost("{serviceId}")]
        public async Task<IActionResult> LinkService(string id, string serviceId)
        {
            var result = await _serviceManager.LinkServiceToProjectAsync(User.GetUserId(), serviceId, id);
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(new { message = "Service linked successfully." });
        }

        [HttpDelete("{serviceId}")]
        public async Task<IActionResult> UnlinkService(string id, string serviceId)
        {
            var result = await _serviceManager.UnlinkServiceFromProjectAsync(User.GetUserId(), serviceId, id);
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(new { message = "Service unlinked successfully." });
        }
    }
}