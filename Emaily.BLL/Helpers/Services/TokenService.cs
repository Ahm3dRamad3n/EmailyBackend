using Emaily.BLL.DTOs.Service;
using Emaily.BLL.Helpers.Interfaces;
using Emaily.DAL.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Emaily.BLL.Helpers.Services
{
    public class TokenService(IConfiguration configuration, ILoggerService logger) : ITokenService
    {
        private readonly ILoggerService _logger = logger;
        private readonly string _secretKey = configuration["JWT_SECRET_KEY"]
                ?? throw new Exception("JWT_SECRET_KEY is missing in .env");
        private readonly string _issuer = configuration["JWT_ISSUER"]
                ?? throw new Exception("JWT_ISSUER is missing in .env");
        private readonly string _audience = configuration["JWT_AUDIENCE"]
                ?? throw new Exception("JWT_AUDIENCE is missing in .env");

        public string GenerateAccessToken(User user, out string jwtId)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Role, user.Roles ?? User.Role.User),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new("purpose", "login")
            };

            jwtId = claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(10),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public string GeneratePasswordResetToken(string userId)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId),
                new("purpose", "password-reset") // تمييز التوكن عشان ميستخدمش في الـ Login
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15), // صالح لمدة 15 دقيقة فقط
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string? ValidatePasswordResetToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _issuer,

                ValidateAudience = true,
                ValidAudience = _audience, 

                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

                var purposeClaim = principal.FindFirst("purpose")?.Value;
                if (purposeClaim != "password-reset")
                {
                    return null; // محاولة اختراق: استخدام توكن Login للـ Reset
                }

                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                return userId;
            }
            catch (Exception)
            {
                _logger.LogDebug(new DTOs.Log(Guid.Empty, $"Invalid or expired password reset token: {token}"));
                return null;
            }
        }
      
        public string GenerateEmailVerificationToken(string email)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Email, email),
                new("purpose", "email_verification")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        
        public string? ValidateEmailVerificationToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _issuer,

                ValidateAudience = true,
                ValidAudience = _audience, 

                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

                var purposeClaim = principal.FindFirst("purpose")?.Value;
                if (purposeClaim != "email_verification")
                {
                    return null; // محاولة اختراق
                }

                var email = principal.FindFirst(ClaimTypes.Email)?.Value;
                return email;
            }
            catch (Exception)
            {
                _logger.LogDebug(new DTOs.Log(Guid.Empty, $"Invalid or expired email verification token: {token}"));
                return null;
            }
        }
    }
}