namespace Emaily.BLL.DTOs.Template
{
    public class TemplateDetailsDto : TemplateDto
    {
        public string? ServiceId { get; set; }
        public string ContentHtml { get; set; } = null!;
        public bool DoSaveInHistory { get; set; }
        public bool EnableRecaptchaV2 { get; set; }
        public string? RecaptchaSecretKey { get; set; }
        public bool EnableAppCheck { get; set; }
        public string? Issuer { get; set; }
        public string? AppCheckSecret { get; set; }
        public string ToEmail { get; set; } = null!;
        public string? ToName { get; set; }
        public string? ReplyTo { get; set; }
        public string? Bcc { get; set; }
        public string? Cc { get; set; }
        public bool EnableAutoReply { get; set; }
        public string? AutoReplyTemplateId { get; set; }

        public List<TemplateAttachmentDto> Attachments { get; set; } = [];
    }
}