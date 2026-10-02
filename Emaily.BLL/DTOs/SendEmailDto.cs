using Emaily.BLL.DTOs.Template;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace Emaily.BLL.DTOs
{
    public class SendEmailDto
    {
        [Required]
        public string ProjectId { get; set; } = null!;

        public string? ServiceId { get; set; }

        [Required, EmailAddress]
        public string ToEmail { get; set; } = null!;    

        public string? ToName { get; set; }

        // جعلنا هذه الحقول اختيارية لأننا قد نعتمد على القالب بدلاً منها
        public string Subject { get; set; } = null!;
        public string Body { get; set; } = null!;

        public string? ReplyTo { get; set; }
        public List<string> Cc { get; set; } = [];
        public List<string> Bcc { get; set; } = [];
        public List<IFormFile> Attachments { get; set; } = [];
    }
}