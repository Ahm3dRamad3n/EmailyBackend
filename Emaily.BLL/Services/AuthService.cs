using Azure.Core;
using Azure.Storage.Blobs.Models;
using Emaily.BLL.DTOs;
using Emaily.BLL.DTOs.Auth;
using Emaily.BLL.Helpers;
using Emaily.BLL.Helpers.Interfaces;
using Emaily.BLL.Interfaces;
using Emaily.DAL.Entities;
using Emaily.DAL.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using static Emaily.BLL.Helpers.Services.TokenService;

namespace Emaily.BLL.Services
{
    public class AuthService(IUnitOfWork uow, ITokenService tokenService, IConfiguration configration, ILoggerService logger, IEmailSenderService emailSenderService) : IAuthService
    {
        private readonly IUnitOfWork _uow = uow;
        private readonly ITokenService _tokenService = tokenService;
        private readonly ILoggerService _logger = logger;
        private readonly IEmailSenderService _emailSenderService = emailSenderService;
        private readonly string _frontendUrl = configration["ClientUrl"] ?? throw new ArgumentNullException("ClientUrl is not configured in appsettings.");

        public async Task<bool> SendVerificationEmailAsync(string email, string target)
        {
            var user = await _uow.Users.FindAsync(u => u.Email == email && !u.IsDeleted);
            if (user != null) return true; // حماية ضد Email Enumeration

            var verificationToken = _tokenService.GenerateEmailVerificationToken(email);
            var urlEncodedToken = Uri.EscapeDataString(verificationToken);
            var verificationLink = $"{_frontendUrl}/src/{target}.html?token={urlEncodedToken}";

            string fullHtmlEmail = EmailTemplateBuilder.GenerateEmailVerificationTemplate(
                email,
                verificationLink
            );

            var response = await _emailSenderService.SendWithSystemAsync(email, email, "Email Verification", fullHtmlEmail, CancellationToken.None, null);
            if (!response.Success)
            {
                _logger.LogError(new Log(Guid.Empty, $"Failed to send verification email to {email}. Error: {response.ErrorMessage}"));
                return false;
            }
            return true;
        }
    
        public async Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto dto)
        {
            string? emailFromToken = _tokenService.ValidateEmailVerificationToken(dto.Token);
            if (string.IsNullOrEmpty(emailFromToken))
                return Result<AuthResponseDto>.Failure("Invalid or expired verification token.", StatusCodes.Status400BadRequest);

            _tokenService.RevokeToken(dto.Token, TokenPurpose.EmailVerification); // إلغاء صلاحية التوكن بعد استخدامه

            var existingUser = await _uow.Users.FindAsync(u => u.Email == emailFromToken && !u.IsDeleted);
            if (existingUser != null)
                return Result<AuthResponseDto>.Failure("Email is already registered.", StatusCodes.Status409Conflict);

            var newUser = new User
            {
                Id = Guid.NewGuid(),
                FullName = dto.FullName,
                Email = emailFromToken.ToLower(),
                DevNotificationEmail = emailFromToken.ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Roles = User.Role.User,
                RemainingQuota = 0, 
                OverageEmails = 0,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                IsDeleted = false
            };

            // 1. إضافة المستخدم للسياق (لم يتم الحفظ في الداتا بيز بعد)
            try
            {
                await _uow.Users.AddAsync(newUser);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(new Log(Guid.Empty, $"Failed to add new user {newUser.Email}. Error: {ex.Message}"));
                return Result<AuthResponseDto>.Failure("Your email has just been registered by another user. Please try logging in.", StatusCodes.Status409Conflict);
            }

            // 2. البحث عن الخطة المجانية (نفترض أن الخطة المجانية سعرها 0)
            var freePlan = await _uow.Plans.FindAsync(p => p.MonthlyPrice == 0 && p.IsActive);

            if (freePlan != null)
            {
                // 3. إنشاء اشتراك للخطة المجانية
                var newSubscription = new Subscription
                {
                    Id = Guid.NewGuid(),
                    UserId = newUser.Id,
                    PlanId = freePlan.Id,
                    Status = Subscription.Statuses.Active,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddMonths(1), // تجديد شهري
                    CreatedAt = DateTime.UtcNow
                };
                await _uow.Subscriptions.AddAsync(newSubscription);

                // 4. تحديث حصة الإيميلات للمستخدم الجديد بناءً على الخطة المجانية
                newUser.RemainingQuota = freePlan.MaxEmailsPerMonth;
            }

            // 5. حفظ كل العمليات (المستخدم + الاشتراك إن وجد) في خطوة واحدة (Transaction)
            await _uow.CompleteAsync();

            return Result<AuthResponseDto>.Success(await GenerateAndSaveTokensAsync(newUser));
        }

        public async Task<Result<AuthResponseDto>> LoginAsync(LoginDto dto)
        {
            var user = await _uow.Users.FindAsync(u => u.Email == dto.Email && !u.IsDeleted);

            if (user == null || !user.IsActive)
                return Result<AuthResponseDto>.Failure("Invalid credentials or account is inactive.", StatusCodes.Status401Unauthorized);

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Result<AuthResponseDto>.Failure("Invalid credentials.", StatusCodes.Status401Unauthorized);

            return Result<AuthResponseDto>.Success(await GenerateAndSaveTokensAsync(user));
        }

        public async Task<Result<AuthResponseDto>> RefreshTokenAsync(string refreshToken)
        {
            var savedToken = await _uow.RefreshTokens.FindAsync(t => t.Token == refreshToken);

            if (savedToken == null || savedToken.IsRevoked || savedToken.IsUsed || savedToken.ExpiresAt <= DateTime.UtcNow)
                return Result<AuthResponseDto>.Failure("Invalid or expired refresh token.", StatusCodes.Status401Unauthorized);

            savedToken.IsUsed = true;
            _uow.RefreshTokens.Update(savedToken);

            var user = await _uow.Users.FindAsync(u => u.Id == savedToken.UserId);

            if (user == null || !user.IsActive || user.IsDeleted)
                return Result<AuthResponseDto>.Failure("User not found or inactive.", StatusCodes.Status401Unauthorized);

            return Result<AuthResponseDto>.Success(await GenerateAndSaveTokensAsync(user));
        }

        public async Task<bool> LogoutAsync(Guid userId, string refreshToken, string accessToken)
        {
            var savedToken = await _uow.RefreshTokens.FindAsync(t => t.Token == refreshToken && t.UserId == userId);

            if (savedToken != null)
            {
                savedToken.IsRevoked = true;
                _uow.RefreshTokens.Update(savedToken);
                await _uow.CompleteAsync();
            }

            _tokenService.RevokeToken(accessToken, TokenPurpose.Login); // إلغاء صلاحية التوكن بعد استخدامه

            return true;
        }
    
        public async Task<bool> ForgotPasswordAsync(string email)
        {
            var user = await _uow.Users.FindAsync(u => u.Email == email && !u.IsDeleted);

            // حماية ضد Email Enumeration
            if (user == null) return true;

            var resetToken = _tokenService.GeneratePasswordResetToken(user.Id.ToString());
            var urlEncodedToken = Uri.EscapeDataString(resetToken);

            var resetLink = $"{_frontendUrl}/src/reset-password.html?token={urlEncodedToken}";

            string fullHtmlEmail = EmailTemplateBuilder.GeneratePasswordResetTemplate(
                user.FullName,
                resetLink
            );

            var response = await _emailSenderService.SendWithSystemAsync(user.FullName, user.DevNotificationEmail, "Security Alert: Password Reset Request", fullHtmlEmail, CancellationToken.None, null);

            if (!response.Success)
            {
                _logger.LogError(new Log(user.Id, $"Failed to send password reset email to {user.DevNotificationEmail}. Error: {response.ErrorMessage}"));
                return false;
            }

            return true;
        }
        
        public async Task<bool> ResetPasswordAsync(ResetPasswordDto model)
        {
            // 1. التحقق من التوكن واستخراج الإيميل أو الـ IpAddress (حسب طريقتك في TokenService)
            // يجب أن تتأكد الدالة أن التوكن لم تنتهِ مدته (الـ 15 دقيقة) وأنه يخص هذا المستخدم
            var userIdString = _tokenService.ValidatePasswordResetToken(model.Token);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                return false;
            }

            _tokenService.RevokeToken(model.Token, TokenPurpose.PasswordReset); // إلغاء صلاحية التوكن بعد استخدامه

            // البحث بالـ IpAddress
            var user = await _uow.Users.FindAsync(u => u.Id == userId && !u.IsDeleted);
            if (user == null)
            {
                return false;
            }

            // 3. تشفير كلمة المرور الجديدة (استخدم دالة التشفير الخاصة بمشروعك)
            // مثال إذا كنت تستخدم BCrypt أو طريقتك الخاصة:
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);

            // 4. تحديث البيانات وحفظها
            _uow.Users.Update(user);
            await _uow.CompleteAsync();

            return true;
        }

        public async Task<bool> DeleteAccountAsync(Guid userId, string accessToken)
        {

            var user = await _uow.Users.FindAsync(u => u.Id == userId);
            if (user == null) return false;

            _tokenService.RevokeToken(accessToken, TokenPurpose.Login); // إلغاء صلاحية التوكن بعد استخدامه

            user.IsDeleted = true;
            user.IsActive = false;
            _uow.Users.Update(user);

            var userTokens = await _uow.RefreshTokens.FindAllAsync(t => t.UserId == userId && !t.IsRevoked);
            foreach (var token in userTokens)
            {
                token.IsRevoked = true;
                _uow.RefreshTokens.Update(token);
            }

            await _uow.CompleteAsync();
            return true;
        }

        private async Task<AuthResponseDto> GenerateAndSaveTokensAsync(User user)
        {
            var accessToken = _tokenService.GenerateAccessToken(user, out var jwtId);
            var refreshToken = _tokenService.GenerateRefreshToken();

            var tokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = refreshToken,
                JwtId = jwtId,
                IsUsed = false,
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            await _uow.RefreshTokens.AddAsync(tokenEntity);
            await _uow.CompleteAsync();

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = 600,
                UserId = user.Id.ToString(),
                FullName = user.FullName,
                Roles = user.Roles
            };
        }
    }
}