using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Emaily.BLL.DTOs
{
    public class SendEmailResponseDto
    {
        public bool Success { get; set; }
        public string FromName { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}