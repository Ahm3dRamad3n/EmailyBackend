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
    [Route("api/services")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting("perUser")]
    public class ServicesController(IServiceManager serviceManager) : ControllerBase
    {
        private readonly IServiceManager _serviceManager = serviceManager;

        [HttpGet]
        public async Task<IActionResult> GetServices()
        {
            var result = await _serviceManager.GetAllServicesAsync(User.GetUserId());
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(result.Data);
        }

        [HttpPost]
        [EnableRateLimiting("StrictUserCreationPolicy")]
        public async Task<IActionResult> AddService([FromBody] CreateServiceDto dto)
        {
            var result = await _serviceManager.AddServiceAsync(User.GetUserId(), dto);
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(result.Data);
        }

        [HttpPut("{serviceId}")]
        [ServiceFilter(typeof(CheckServiceOwnershipFilter))]
        public async Task<IActionResult> UpdateService(string serviceId, [FromBody] UpdateServiceDto dto)
        {
            var result = await _serviceManager.UpdateServiceAsync(serviceId, dto);
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(result.Data);
        }

        [HttpDelete("{serviceId}")]
        [ServiceFilter(typeof(CheckServiceOwnershipFilter))]
        public async Task<IActionResult> DeleteService(string serviceId)
        {
            var deleted = await _serviceManager.DeleteServiceAsync(serviceId);
            if (!deleted) return NotFound(new { message = "Service not found or access denied." });
            return Ok(new { message = "Service deleted successfully." });
        }

        [HttpPut("{serviceId}/status")]
        [ServiceFilter(typeof(CheckServiceOwnershipFilter))]
        public async Task<IActionResult> ToggleStatus(string serviceId, [FromBody] Emaily.BLL.DTOs.UpdateStatusDto dto)
        {
            var success = await _serviceManager.ToggleStatusAsync(serviceId, dto.IsActive);
            if (!success) return NotFound(new { message = "Service not found or access denied." });
            return Ok(new { message = "Service status updated successfully." });
        }

        [HttpPut("{serviceId}/unlock")]
        [EnableRateLimiting("StrictUserCreationPolicy")]
        [ServiceFilter(typeof(CheckServiceOwnershipFilter))]
        public async Task<IActionResult> UnlockService(string serviceId)
        {
            var result = await _serviceManager.UnlockServiceAsync(serviceId);
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(new { message = "Service unlocked successfully." });
        }
    }
}