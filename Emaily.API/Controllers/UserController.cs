using Emaily.API.Extensions;
using Emaily.BLL.DTOs.User;
using Emaily.BLL.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Emaily.API.Controllers
{
    [Route("api/user")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting("perUser")]
    public class UserController(IUserService userService) : ControllerBase
    {
        private readonly IUserService _userService = userService;

        [HttpGet("me")]
        public async Task<IActionResult> GetProfile()
        {
            var result = await _userService.GetProfileAsync(User.GetUserId());
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(result.Data);
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _userService.UpdateProfileAsync(User.GetUserId(), dto);
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(result.Data);
        }

        [HttpDelete("me")]
        public async Task<IActionResult> DeleteAccount()
        {
            string? accessToken = await HttpContext.GetTokenAsync("access_token");
            if (string.IsNullOrEmpty(accessToken))
            {
                return BadRequest(new { message = "Access token is missing." });
            }
            var deleted = await _userService.DeleteAccountAsync(User.GetUserId(), accessToken);
            if (!deleted) return NotFound(new { message = "User not found or already deleted." });
            return Ok(new { message = "Account deleted successfully and all tokens revoked." });
        }

        [HttpPut("password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _userService.ChangePasswordAsync(User.GetUserId(), dto);
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(new { message = "Password changed successfully. Please login again." });
        }

        [HttpGet("quota")]
        public async Task<IActionResult> GetQuota()
        {
            var result = await _userService.GetQuotaAsync(User.GetUserId());
            if (!result.IsSuccess) return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(result.Data);
        }
    }
}