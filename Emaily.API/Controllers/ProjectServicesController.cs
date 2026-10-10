using Emaily.API.Extensions;
using Emaily.API.Filters;
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
    [Route("api/projects/{projectId}/services")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting("perUser")]
    [ServiceFilter(typeof(CheckProjectOwnershipFilter))]
    public class ProjectServicesController(IServiceManager serviceManager) : ControllerBase
    {
        private readonly IServiceManager _serviceManager = serviceManager;

        [HttpGet]
        public async Task<IActionResult> GetServices(string projectId)
        {
            var services = await _serviceManager.GetProjectServicesAsync(projectId);
            if (!services.IsSuccess) return StatusCode(services.ErrorCode, new { success = false, message = services.ErrorMessage });
            return Ok(services.Data);
        }

        [HttpPost("{serviceId}")]
        [ServiceFilter(typeof(CheckServiceOwnershipFilter))]
        public async Task<IActionResult> LinkService(string projectId, string serviceId)
        {
            var result = await _serviceManager.LinkServiceToProjectAsync(serviceId, projectId);
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(new { message = "Service linked successfully." });
        }

        [HttpDelete("{serviceId}")]
        [ServiceFilter(typeof(CheckServiceOwnershipFilter))]
        public async Task<IActionResult> UnlinkService(string projectId, string serviceId)
        {
            var result = await _serviceManager.UnlinkServiceFromProjectAsync(serviceId, projectId);
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(new { message = "Service unlinked successfully." });
        }
    }
}