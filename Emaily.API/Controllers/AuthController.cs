using Azure.Core;
using Emaily.API.Extensions;
using Emaily.BLL.DTOs.Auth;
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
    [Route("api/auth")]
    [ApiController]
    [AllowAnonymous]
    [EnableRateLimiting("forOAuth")] 
    public class AuthController(IAuthService authService) : ControllerBase
    {
        private readonly IAuthService _authService = authService;

        [HttpPost("send-verification-email")]
        public async Task<IActionResult> SendVerificationEmail([FromBody] SendVerificationEmailDto dto)
        {
            await _authService.SendVerificationEmailAsync(dto.Email);
            return Ok(new { message = "If the email exists, a verification link has been sent." });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var result = await _authService.RegisterAsync(dto);
            if (!result.IsSuccess)
                return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(result.Data);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var result = await _authService.LoginAsync(dto);
            if (!result.IsSuccess)
                return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(result.Data);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto)
        {
            var result = await _authService.RefreshTokenAsync(dto.Token);
            if (!result.IsSuccess)
                return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            return Ok(result.Data);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] LogoutRequestDto dto)
        {
            var accessToken = await HttpContext.GetTokenAsync("access_token");
            if (string.IsNullOrEmpty(accessToken))
            {
                return BadRequest(new { message = "Access token is missing." });
            }
            await _authService.LogoutAsync(User.GetUserId(), dto.RefreshToken, accessToken); 
            return Ok(new { message = "Logged out successfully" });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            await _authService.ForgotPasswordAsync(dto.Email);
            return Ok(new { message = "If the email exists, a reset link has been sent." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var isResetSuccessful = await _authService.ResetPasswordAsync(request);

            if (!isResetSuccessful)
            {
                return BadRequest(new { Message = "Invalid or expired password reset token." });
            }

            return Ok(new { Message = "Password has been reset successfully." });
        }

        [HttpDelete("delete-account")]
        [Authorize]
        public async Task<IActionResult> DeleteAccount()
        {
            await _authService.DeleteAccountAsync(User.GetUserId());
            return Ok(new { message = "Account deleted successfully" });
        }
    }
}