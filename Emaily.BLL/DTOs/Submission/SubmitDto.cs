using Emaily.BLL.Attributes;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Emaily.BLL.DTOs.Submission
{
    public class SubmitDto
    {
        [RequiredPrefix("t_")]
        public string TemplateId { get; set; } = null!;
       
        [RequiredPrefix("sec_", AllowNull: true)]
        public string? PrivateKey { get; set; }

        [ValidEmail(AllowNull: true)]
        public string? RecipientEmail { get; set; }
        public string? RecipientName { get; set; }
        public string? RecaptchaToken { get; set; }
        public string? AppCheckToken { get; set; }

        // المتغيرات التي سيتم حقنها في القالب (مثل {{FullName}})
        public Dictionary<string, string> Fields { get; set; } = [];
    }
}

