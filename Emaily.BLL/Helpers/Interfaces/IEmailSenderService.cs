using Emaily.BLL.DTOs;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Emaily.BLL.Helpers.Interfaces
{
    public interface IEmailSenderService
    {
        Task<SendEmailResponseDto> NotifyAdminAsync(string subject, string body, string? replyTo, CancellationToken cancellationToken, FileDto? dto = null);
        Task<SendEmailResponseDto> SendWithSystemAsync(string fullName, string toEmail, string subject, string body, CancellationToken cancellationToken, SendEmailDto? dto = null);
        Task<SendEmailResponseDto> SendEmailAsync(SendEmailDto dto);
    }
}