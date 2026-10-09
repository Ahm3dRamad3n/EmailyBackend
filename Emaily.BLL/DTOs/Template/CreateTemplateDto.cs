using Emaily.BLL.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Emaily.BLL.DTOs.Template
{
    public class CreateTemplateDto
    {
        [RequiredPrefix("s_", AllowNull: true)] public string? ServiceId { get; set; } // اختياري
        [RequiredText, StringLength(100, MinimumLength = 3)] public string Name { get; set; } = null!;
        [RequiredText, StringLength(255, MinimumLength = 3)] public string Subject { get; set; } = null!;
        [RequiredText] public string ContentHtml { get; set; } = null!;
        public bool DoSaveInHistory { get; set; } = true;
        public bool EnableRecaptchaV2 { get; set; }
        public string? RecaptchaSecretKey { get; set; }
        public bool EnableAppCheck { get; set; }
        public string? Issuer { get; set; }
        public string? AppCheckSecret { get; set; }
        [RequiredText] public string ToEmail { get; set; } = null!;
        public string? ToName { get; set; }
        public string? ReplyTo { get; set; }
        [ValidEmail(AllowNull: true)] public string? Bcc { get; set; }
        [ValidEmail(AllowNull: true)] public string? Cc { get; set; }
        public bool EnableAutoReply { get; set; }
        [RequiredPrefix("t_", AllowNull: true)] public string? AutoReplyTemplateId { get; set; }
    }
}