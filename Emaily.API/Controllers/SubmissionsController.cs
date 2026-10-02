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
    [Route("api/submissions")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting("perUser")]
    public class SubmissionsController(ISubmissionService submissionService) : ControllerBase
    {
        private readonly ISubmissionService _submissionService = submissionService;

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSubmissionDetails(string id)
        {
            var result = await _submissionService.GetSubmissionDetailsAsync(User.GetUserId(), id);
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(result.Data);
        }

        [HttpGet("{id}/status")]
        public async Task<IActionResult> GetSubmissionStatus(string id)
        {
            var result = await _submissionService.GetSubmissionStatusAsync(User.GetUserId(), id);
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage }); 
            return Ok(result.Data);
        }

        [HttpPost("{id}/retry")]
        public async Task<IActionResult> RetrySubmission(string id)
        {
            var result = await _submissionService.RetrySubmissionAsync(User.GetUserId(), id);
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(result.Data);
        }
    }
}