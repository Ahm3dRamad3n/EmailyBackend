using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Emaily.BLL.Interfaces;
using System.Threading.Tasks;
using System;

namespace Emaily.API.Controllers
{
    [Route("api/webhooks/bot")]
    [ApiController]
    [DisableRateLimiting]
    [ServiceFilter(typeof(InternalMasterApiKeyFilter))]
    public class BotWebhookController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IProjectsService _projectService;
        private readonly ISubmissionService _submissionService;
        private readonly IIntegrationService _integrationService;

        public BotWebhookController(
            IUserService userService,
            IProjectsService projectService,
            ISubmissionService submissionService,
            IIntegrationService integrationService)
        {
            _userService = userService;
            _projectService = projectService;
            _submissionService = submissionService;
            _integrationService = integrationService;
        }

        [HttpGet("quota")]
        public async Task<IActionResult> GetQuota([FromQuery] long chatId)
        {
            var userIds = await _integrationService.GetUsersIdByChatIdAsync(chatId.ToString());

            if (userIds == null || !userIds.Any())
                return NotFound(new { text = "Your account is not linked to the system." });

            var users = new List<object>();

            foreach (var userId in userIds)
            {
                var user = await _userService.GetProfileAsync(userId);
                if (user.IsSuccess && user.Data != null)
                {
                    users.Add(user.Data);
                }
            }

            if (!users.Any())
            {
                return NotFound("No active users found for your chat Bot.");
            }

            return Ok(users);
        }

        [HttpGet("projects")]
        public async Task<IActionResult> GetProjects([FromQuery] long chatId, [FromQuery] Guid userId)
        {
            var projectIds = await _integrationService.GetProjectsIdByChatIdAndUserIdAsync(chatId.ToString(), userId);

            if (projectIds == null || !projectIds.Any())
                return NotFound("Your account is not linked to the system.");

            var allProjects = new List<object>();

            foreach (var projectId in projectIds)
            {
                var userProject = await _projectService.GetByIdAsync(projectId);

                if (userProject.IsSuccess && userProject.Data != null)
                {
                    allProjects.Add(userProject.Data);
                }
            }

            if (!allProjects.Any())
            {
                return NotFound("No active projects found for your accounts.");
            }

            return Ok(allProjects);
        }

        [HttpGet("submission/{submissionId}")]
        public async Task<IActionResult> GetSubmission(string submissionId)
        {
            var result = await _submissionService.GetSubmissionDetailsAsync(submissionId);
            if (!result.IsSuccess)
                return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(result.Data);
        }

        [HttpPost("submission/{submissionId}/retry")]
        public async Task<IActionResult> RetrySubmission(Guid userId, string submissionId)
        {
            var result = await _submissionService.RetrySubmissionAsync(userId, submissionId);
            if (!result.IsSuccess)
                return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(result.Data);
        }

        [HttpGet("submissions/{projectId}")]
        public async Task<IActionResult> GetAllSubmissions(string projectId, [FromQuery] int page)
        {
            var result = await _submissionService.GetProjectSubmissionsAsync(projectId, page);
            if (!result.IsSuccess)
                return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(result.Data);
        }
    }
}