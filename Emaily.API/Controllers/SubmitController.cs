using Emaily.API.Extensions;
using Emaily.BLL.DTOs.Submission;
using Emaily.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Threading.Tasks;

namespace Emaily.API.Controllers
{
    [Route("api")]
    [ApiController]
    [EnableCors("AllowAllOrigins")]
    [EnableRateLimiting("forSubmit")]
    [AllowAnonymous]
    public class SubmitController(ISubmissionService submissionService) : ControllerBase
    {
        private readonly ISubmissionService _submissionService = submissionService;

        [HttpPost("submit/{PublicApiKey}")]
        [RequestSizeLimit(3 * 1024 * 1024)] // 3 MB
        public async Task<IActionResult> Submit(string PublicApiKey, [FromBody] SubmitDto dto)
        {
            var origin = HttpContext.GetOrigin();
            var response = await _submissionService.SubmitAsync(PublicApiKey, dto, origin);
            if (!response.IsSuccess) return StatusCode(response.ErrorCode, new { success = false, message = response.ErrorMessage });
            return Ok(new { message = "The email has been successfully queued.", status = "Success" });
        }

        [HttpPost("support")]
        [RequestSizeLimit(5 * 1024 * 1024)] // 5 MB
        public async Task<IActionResult> Support([FromForm] SupportDto dto)
        {
            var origin = HttpContext.GetOrigin();
            var response = await _submissionService.SubmitSupportAsync(dto, origin);
            if (!response.IsSuccess) return StatusCode(response.ErrorCode, new { success = false, message = response.ErrorMessage });
            return Ok(new { message = "Your support request has been successfully submitted.", status = "Success" });
        }
    }
}