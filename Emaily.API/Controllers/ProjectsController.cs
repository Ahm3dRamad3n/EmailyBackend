using Emaily.API.Extensions;
using Emaily.API.Filters;
using Emaily.BLL.DTOs.Project;
using Emaily.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Emaily.API.Controllers
{
    [Route("api/projects")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting("perUser")]
    public class ProjectsController(IProjectsService projectService) : ControllerBase
    {
        private readonly IProjectsService _projectService = projectService;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var projects = await _projectService.GetAllProjectsAsync(User.GetUserId());
            return Ok(projects);
        }

        [HttpPost]
        [EnableRateLimiting("StrictUserCreationPolicy")]
        public async Task<IActionResult> Create([FromBody] CreateProjectDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _projectService.CreateAsync(User.GetUserId(), dto);
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(result.Data);
        }

        [HttpGet("{projectId}")]
        [ServiceFilter(typeof(CheckProjectOwnershipFilter))]
        public async Task<IActionResult> GetById(string projectId)
        {
            var project = await _projectService.GetByIdAsync(projectId);
            if (!project.IsSuccess) return StatusCode(project.ErrorCode, new { success = false, message = project.ErrorMessage });
            return Ok(project.Data);
        }

        [HttpPut("{projectId}")]
        [ServiceFilter(typeof(CheckProjectOwnershipFilter))]
        public async Task<IActionResult> Update(string projectId, [FromBody] UpdateProjectDto dto)
        {
            var result = await _projectService.UpdateAsync(projectId, dto);
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(result.Data);
        }

        [HttpPost("{projectId}/keys")]
        [ServiceFilter(typeof(CheckProjectOwnershipFilter))]
        public async Task<IActionResult> RegenerateKeys(string projectId)
        {
            var result = await _projectService.RegenerateKeysAsync(projectId);
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(new { message = "Keys regenerated successfully.", data = result.Data });
        }

        [HttpDelete("{projectId}")]
        [ServiceFilter(typeof(CheckProjectOwnershipFilter))]
        public async Task<IActionResult> Delete(string projectId)
        {
            var deleted = await _projectService.DeleteAsync(projectId);
            if (!deleted) return NotFound(new { message = "Project not found or already deleted." });

            return Ok(new { message = "Project deleted successfully." });
        }

        [HttpPut("{projectId}/status")]
        [ServiceFilter(typeof(CheckProjectOwnershipFilter))]
        public async Task<IActionResult> ToggleStatus(string projectId, [FromBody] Emaily.BLL.DTOs.UpdateStatusDto dto)
        {
            var success = await _projectService.ToggleStatusAsync(projectId, dto.IsActive);
            if (!success) return NotFound(new { message = "Project not found or access denied." });
            return Ok(new { message = "Project status updated successfully." });
        }

        [HttpPut("{projectId}/access-mode")]
        [ServiceFilter(typeof(CheckProjectOwnershipFilter))]
        public async Task<IActionResult> ChangeAccessMode(string projectId, [FromBody] ChangeAccessModeDto dto)
        {
            var result = await _projectService.ChangeAccessModeAsync(projectId, dto);
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { seccess = false, message = result.ErrorMessage });
            return Ok(new { success = true, message = "Project access mode updated successfully." });
        }

        [HttpPut("{projectId}/unlock")]
        [EnableRateLimiting("StrictUserCreationPolicy")]
        [ServiceFilter(typeof(CheckProjectOwnershipFilter))]
        public async Task<IActionResult> UnlockProject(string projectId)
        {
            var result = await _projectService.UnlockProjectAsync(projectId);
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(new { message = "Project unlocked successfully." });
        }
    }
}