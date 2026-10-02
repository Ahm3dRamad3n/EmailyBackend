using Emaily.API.Extensions;
using Emaily.BLL.DTOs.Admin;
using Emaily.BLL.DTOs.Billing;
using Emaily.BLL.Interfaces;
using Emaily.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.Tasks;

namespace Emaily.API.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    [DisableRateLimiting] // حماية إضافية لضمان عدم تطبيق أي قيود هنا نهائياً
    public class AdminController(IAdminService adminService) : ControllerBase
    {
        private readonly IAdminService _adminService = adminService;

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            return Ok(await _adminService.GetDashboardStatsAsync());
        }
       
        [HttpGet("analytics")]
        public async Task<IActionResult> GetSystemAnalytics()
        {
            return Ok(await _adminService.GetSystemAnalyticsAsync());
        }


        // users
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] int page = 1)
        {
            var users = await _adminService.GetUsersAsync(page);
            return Ok(users);
        }

        [HttpGet("users/search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string searchTerm, [FromQuery] int page = 1)
        {
            var results = await _adminService.SearchUsersAsync(searchTerm, page);
            return Ok(results);
        }

        [HttpGet("users/{userId}/details")]
        public async Task<IActionResult> GetUserDetails(string userId)
        {
            var userDetails = await _adminService.GetUserDetailsAsync(userId);
            if (userDetails == null) return NotFound(new { message = "User not found." });
            return Ok(userDetails);
        }

        [HttpPut("users/{id}/status")]
        public async Task<IActionResult> ToggleUserStatus(string id, [FromBody] Emaily.BLL.DTOs.UpdateStatusDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var success = await _adminService.ToggleUserStatusAsync(id, dto.IsActive);
            if (!success) return NotFound(new { message = "User not found." });
            return Ok(new { message = "User status updated." });
        }
        
        [HttpGet("users/{id}/projects")]
        public async Task<IActionResult> GetUserProjects(string id)
        {
            return Ok(await _adminService.GetUserProjectsAsync(id));
        }


        // banned
        [HttpGet("banned")]
        public async Task<IActionResult> GetBannedList([FromQuery] int page = 1)
        {
            return Ok(await _adminService.GetBannedListAsync(page));
        }

        [HttpPost("banned")]
        public async Task<IActionResult> AddBan([FromBody] AddBanDto dto)
        {
            await _adminService.AddBanAsync(dto, User.GetUserId());
            return Ok(new { message = "Ban record added/updated." });
        }

        [HttpDelete("banned")]
        public async Task<IActionResult> RemoveBan([FromBody] BanDto dto)
        {
            var success = await _adminService.RemoveBanAsync(dto, User.GetUserId());
            if (!success) return NotFound(new { message = "Ban record not found." });
            return Ok(new { message = "Ban removed." });
        }

        [HttpPut("banned")]
        public async Task<IActionResult> ReduceBan([FromBody] BanDto dto)
        {
            var success = await _adminService.ReduceBanAsync(dto, User.GetUserId());
            if (!success) return NotFound(new { message = "Ban record not found." });
            return Ok(new { message = "Ban severity reduced." });
        }

        [HttpGet("banned/{ip}")]
        public async Task<IActionResult> GetBanDetails(string ip)
        {
            return Ok(await _adminService.GetBanDetailsByIP(ip));
        }

        [HttpGet("banned/search")]
        public async Task<IActionResult> SearchBanned([FromQuery] string searchTerm, [FromQuery] int page = 1)
        {
            var results = await _adminService.SearchBannedListAsync(searchTerm, page);
            return Ok(results);
        }


        // plans
        [HttpPost("plans")]
        public async Task<IActionResult> CreateOrUpdatePlan([FromBody] CreatePlanDto dto)
        {
            await _adminService.CreateOrUpdatePlanAsync(dto);
            return Ok(new { message = "Plan created or updated successfully." });
        }

        
        // projects
        [HttpGet("projects")]
        public async Task<IActionResult> GetProjects([FromQuery] int page = 1)
        {
            return Ok(await _adminService.GetAdminProjectsAsync(page));
        }

        [HttpGet("projects/search")]
        public async Task<IActionResult> SearchProjects([FromQuery] string searchTerm, [FromQuery] int page = 1)
        {
            var results = await _adminService.SearchProjectsAsync(searchTerm, page);
            return Ok(results);
        }

        [HttpGet("projects/{id}/templates")]
        public async Task<IActionResult> GetProjectTemplates(string id)
        {
            return Ok(await _adminService.GetProjectTemplatesAsync(id));
        }

        [HttpGet("projects/{id}/services")]
        public async Task<IActionResult> GetProjectServices(string id)
        {
            return Ok(await _adminService.GetProjectServicesAsync(id));
        }
       

        // templates
        [HttpGet("templates")]
        public async Task<IActionResult> GetTemplates([FromQuery] int page = 1)
        {
            return Ok(await _adminService.GetAdminTemplatesAsync(page));
        }

        [HttpGet("templates/search")]
        public async Task<IActionResult> SearchTemplates([FromQuery] string searchTerm, [FromQuery] int page = 1)
        {
            var results = await _adminService.SearchTemplatesAsync(searchTerm, page);
            return Ok(results);
        }

        [HttpGet("templates/{id}")]
        public async Task<IActionResult> GetTemplateById(string id)
        {
            var template = await _adminService.GetTemplateByIdAsync(id);
            if (template == null) return NotFound(new { message = "Template not found." });
            return Ok(template);
        }


        // services
        [HttpGet("services")]
        public async Task<IActionResult> GetServices([FromQuery] int page = 1)
        {
            return Ok(await _adminService.GetAdminServicesAsync(page));
        }

        [HttpGet("services/search")]
        public async Task<IActionResult> SearchServices([FromQuery] string searchTerm, [FromQuery] int page = 1)
        {
            var results = await _adminService.SearchServicesAsync(searchTerm, page);
            return Ok(results);
        }

        [HttpGet("services/{id}")]
        public async Task<IActionResult> GetServiceById(string id)
        {
            var service = await _adminService.GetServiceByIdAsync(id);
            if (service == null) return NotFound(new { message = "Service not found." });
            return Ok(service);
        }


        // logs
        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs([FromQuery] int page = 1)
        {
            return Ok(await _adminService.GetSystemLogsAsync(page));
        }

        [HttpGet("logs/search")]
        public async Task<IActionResult> SearchLogs([FromQuery] string searchTerm, [FromQuery] int page = 1)
        {
            var results = await _adminService.SearchSystemLogsAsync(searchTerm, page);
            return Ok(results);
        }


        // subscribe a user to a plan
        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] AdminSubscribeRequestDto dto)
        {
             var response = await _adminService.SubscribeAsync(dto);
            if (!response.IsSuccess)
                return StatusCode(response.ErrorCode, new
                {
                    success = false,
                    message = response.ErrorMessage
                });
            return Ok(new { success = true, message = "User subscribed successfully." });
        }
    }
}