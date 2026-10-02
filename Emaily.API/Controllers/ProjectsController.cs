using Emaily.API.Extensions;
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
        public async Task<IActionResult> Create([FromBody] CreateProjectDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _projectService.CreateAsync(User.GetUserId(), dto);
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(result.Data);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var project = await _projectService.GetByIdAsync(User.GetUserId(), id);
            if (!project.IsSuccess) return StatusCode(project.ErrorCode, new { success = false, message = project.ErrorMessage });
            return Ok(project.Data);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateProjectDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _projectService.UpdateAsync(User.GetUserId(), id, dto);
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(result.Data);
        }

        [HttpPost("{id}/keys")]
        public async Task<IActionResult> RegenerateKeys(string id)
        {
            var result = await _projectService.RegenerateKeysAsync(User.GetUserId(), id);
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(new { message = "Keys regenerated successfully.", data = result.Data });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var deleted = await _projectService.DeleteAsync(User.GetUserId(), id);
            if (!deleted) return NotFound(new { message = "Project not found or already deleted." });

            return Ok(new { message = "Project deleted successfully." });
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> ToggleStatus(string id, [FromBody] Emaily.BLL.DTOs.UpdateStatusDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var success = await _projectService.ToggleStatusAsync(User.GetUserId(), id, dto.IsActive);
            if (!success) return NotFound(new { message = "Project not found or access denied." });
            return Ok(new { message = "Project status updated successfully." });
        }

        [HttpPut("{id}/access-mode")]
        public async Task<IActionResult> ChangeAccessMode(string id, [FromBody] ChangeAccessModeDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _projectService.ChangeAccessModeAsync(User.GetUserId(), id, dto);
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { seccess = false, message = result.ErrorMessage });
            return Ok(new { success = true, message = "Project access mode updated successfully." });
        }

        [HttpPut("{id}/unlock")]
        public async Task<IActionResult> UnlockProject(string id)
        {
            var result = await _projectService.UnlockProjectAsync(User.GetUserId(), id);
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(new { message = "Project unlocked successfully." });
        }
    }
}