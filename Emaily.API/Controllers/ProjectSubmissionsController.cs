using Emaily.API.Extensions;
using Emaily.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Emaily.API.Controllers
{
    [Route("api/projects/{id}/submissions")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting("perUser")]
    public class ProjectSubmissionsController(ISubmissionService submissionService) : ControllerBase
    {
        private readonly ISubmissionService _submissionService = submissionService;

        [HttpGet]
        public async Task<IActionResult> GetSubmissions(string id, [FromQuery] int page = 1)
        {
            var result = await _submissionService.GetProjectSubmissionsAsync(User.GetUserId(), id, page);
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(result.Data);
        }
    }
}