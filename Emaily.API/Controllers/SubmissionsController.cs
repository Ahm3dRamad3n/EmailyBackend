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
    [Route("api/submissions/{submissionId}")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting("perUser")]
    [ServiceFilter(typeof(CheckSubmissionOwnershipFilter))]
    public class SubmissionsController(ISubmissionService submissionService) : ControllerBase
    {
        private readonly ISubmissionService _submissionService = submissionService;

        [HttpGet]
        public async Task<IActionResult> GetSubmissionDetails(string submissionId)
        {
            var result = await _submissionService.GetSubmissionDetailsAsync(submissionId);
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(result.Data);
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetSubmissionStatus(string submissionId)
        {
            var result = await _submissionService.GetSubmissionStatusAsync(submissionId);
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage }); 
            return Ok(result.Data);
        }

        [HttpPost("retry")]
        public async Task<IActionResult> RetrySubmission(string submissionId)
        {
            var result = await _submissionService.RetrySubmissionAsync(User.GetUserId(), submissionId);
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(result.Data);
        }
    }
}