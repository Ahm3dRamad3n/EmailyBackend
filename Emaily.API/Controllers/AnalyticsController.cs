using Emaily.API.Extensions;
using Emaily.API.Filters;
using Emaily.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Emaily.API.Controllers
{
    [Route("api/analytics")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting("perUser")]
    public class AnalyticsController(IAnalyticsService analyticsService) : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService = analyticsService;

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview()
        {
            var result = await _analyticsService.GetOverviewAnalyticsAsync(User.GetUserId());
            return Ok(result);
        }

        [HttpGet("projects/{projectId}")]
        [ServiceFilter(typeof(CheckProjectOwnershipFilter))]
        public async Task<IActionResult> GetProjectAnalytics(string projectId)
        {
            var result = await _analyticsService.GetProjectAnalyticsAsync(projectId);
            if (!result.IsSuccess)
                return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(result.Data);
        }
    }
}