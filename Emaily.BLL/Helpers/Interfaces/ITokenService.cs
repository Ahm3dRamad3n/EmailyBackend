using Emaily.DAL.Entities;
using System.Security.Claims;
using System.Collections.Generic;
using static Emaily.BLL.Helpers.Services.TokenService;

namespace Emaily.BLL.Helpers.Interfaces
{
    public interface ITokenService
    {
        string GenerateAccessToken(User user, out string jwtId);
        string GenerateRefreshToken();
        string GeneratePasswordResetToken(string userId);
        string? ValidatePasswordResetToken(string token);
        string GenerateEmailVerificationToken(string email);
        string? ValidateEmailVerificationToken(string token);
        void RevokeToken(string token, TokenPurpose purpose);
        bool IsTokenRevoked(string token, TokenPurpose purpose);

    }
}