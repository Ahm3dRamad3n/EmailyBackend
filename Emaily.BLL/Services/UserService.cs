using Emaily.BLL.DTOs;
using Emaily.BLL.DTOs.User;
using Emaily.BLL.Helpers.Interfaces;
using Emaily.BLL.Interfaces;
using Emaily.DAL.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Emaily.BLL.Services
{
    public class UserService(IUnitOfWork uow, ITokenService tokenService, IAuthService authService
        ) : IUserService
    {
        private readonly IUnitOfWork _uow = uow;
        private readonly ITokenService _tokenService = tokenService;
        private readonly IAuthService _authService = authService;

        public async Task<Result<UserProfileDto>> GetProfileAsync(Guid userId)
        {
            var user = await _uow.Users.FindAsync(u => u.Id == userId && !u.IsDeleted);
            if (user == null) return Result<UserProfileDto>.Failure("User not found.", StatusCodes.Status404NotFound);

            return Result<UserProfileDto>.Success(
            new UserProfileDto
            {
                Id = user.Id.ToString(),
                FullName = user.FullName,
                Email = user.Email,
                DevNotificationEmail = user.DevNotificationEmail,
                RemainingQuota = user.RemainingQuota,
                OverageEmails = user.OverageEmails,
                Roles = user.Roles
            });
        }

        public async Task<Result<UserProfileDto>> UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
        {
            var user = await _uow.Users.FindAsync(u => u.Id == userId && !u.IsDeleted);
            if (user == null) return Result<UserProfileDto>.Failure("User not found.", StatusCodes.Status404NotFound);

            if (user.DevNotificationEmail != dto.DevNotificationEmail)
            {
                if (string.IsNullOrEmpty(dto.Token))
                {
                    return Result<UserProfileDto>.Failure("Token is required to change the developer notification email.", StatusCodes.Status400BadRequest);
                }
                if (_tokenService.IsTokenRevoked(dto.Token, Helpers.Services.TokenService.TokenPurpose.EmailVerification))
                {
                    return Result<UserProfileDto>.Failure("Invalid token or expired for changing the developer notification email.", StatusCodes.Status400BadRequest);
                }

                _tokenService.RevokeToken(dto.Token, Helpers.Services.TokenService.TokenPurpose.EmailVerification);
                
                string? tokenEmail = _tokenService.ValidateEmailVerificationToken(dto.Token);
                if (tokenEmail == null || tokenEmail != dto.DevNotificationEmail)
                {
                    return Result<UserProfileDto>.Failure("Invalid token or email mismatch for changing the developer notification email.", StatusCodes.Status400BadRequest);
                }
            }

            user.FullName = dto.FullName;
            user.DevNotificationEmail = dto.DevNotificationEmail;

            _uow.Users.Update(user);
            await _uow.CompleteAsync();

            return Result<UserProfileDto>.Success(
            new UserProfileDto
            {
                Id = user.Id.ToString(),
                FullName = user.FullName,
                Email = user.Email,
                DevNotificationEmail = user.DevNotificationEmail,
                RemainingQuota = user.RemainingQuota,
                OverageEmails = user.OverageEmails,
                Roles = user.Roles
            });
        }

        public async Task<Result<bool>> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
        {
            var user = await _uow.Users.FindAsync(u => u.Id == userId && !u.IsDeleted);
            if (user == null) return Result<bool>.Failure("User not found.", StatusCodes.Status404NotFound);

            // التحقق من كلمة المرور الحالية
            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                return Result<bool>.Failure("Current password is incorrect.", StatusCodes.Status400BadRequest);

            // تشفير وتحديث كلمة المرور الجديدة
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

            _uow.Users.Update(user);

            // إبطال كل الـ Refresh Tokens الفعالة لكي يتم تسجيل خروج المستخدم من جميع الأجهزة كإجراء أمني
            var userTokens = await _uow.RefreshTokens.FindAllAsync(t => t.UserId == userId && !t.IsRevoked);
            foreach (var token in userTokens)
            {
                token.IsRevoked = true;
                _uow.RefreshTokens.Update(token);
            }

            await _uow.CompleteAsync();
            return Result<bool>.Success(true);
        }

        public async Task<Result<UserQuotaDto>> GetQuotaAsync(Guid userId)
        {
            var quota = await _uow.Users.SelectWhereAsync(selector: u => new UserQuotaDto
            {
                RemainingQuota = u.RemainingQuota,
                OverageEmails = u.OverageEmails
            }, criteria: u => u.Id == userId && !u.IsDeleted);

            if (quota == null) return Result<UserQuotaDto>.Failure("User not found.", StatusCodes.Status404NotFound);

            return Result<UserQuotaDto>.Success(quota.First());
        }

        public async Task<bool> DeleteAccountAsync(Guid userId, string accessToken)
        {
            return await _authService.DeleteAccountAsync(userId, accessToken);
        }
    }
}