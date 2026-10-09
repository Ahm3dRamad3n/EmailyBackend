using Emaily.BLL.DTOs;
using Emaily.BLL.DTOs.Auth;

namespace Emaily.BLL.Interfaces
{
    public interface IAuthService
    {
        Task<bool> SendVerificationEmailAsync(string email, string target);
        Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto dto);
        Task<Result<AuthResponseDto>> LoginAsync(LoginDto dto);
        Task<Result<AuthResponseDto>> RefreshTokenAsync(string refreshToken);
        Task<bool> LogoutAsync(Guid userId, string refreshToken, string accessToken);
        Task<bool> ForgotPasswordAsync(string email);
        Task<bool> ResetPasswordAsync(ResetPasswordDto dto);
        Task<bool> DeleteAccountAsync(Guid userId, string accessToken);
    }
}