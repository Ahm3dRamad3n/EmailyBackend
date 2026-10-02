using System;
using System.Collections.Generic;

namespace Emaily.DAL.Entities;

public partial class Template
{
    public int ClusterKey { get; set; }

    public string Id { get; set; } = null!;

    public string ProjectId { get; set; } = null!;

    public string? ServiceId { get; set; }

    public string Name { get; set; } = null!;

    public string Subject { get; set; } = null!;

    public string ContentHtml { get; set; } = null!;

    public bool DoSaveInHistory { get; set; }

    public bool EnableRecaptchaV2 { get; set; }

    public string? RecaptchaSecretKey { get; set; }

    public bool EnableAppCheck { get; set; }

    public string? AppCheckSecret { get; set; }

    public string? Issuer { get; set; }

    public string ToEmail { get; set; } = null!;

    public string? ToName { get; set; }

    public string? ReplyTo { get; set; }

    public string? Bcc { get; set; }

    public string? Cc { get; set; }

    public bool EnableAutoReply { get; set; }

    public string? AutoReplyTemplateId { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsActive { get; set; }

    public bool IsDeleted { get; set; }

    public bool IsLocked { get; set; }

    public virtual Template? AutoReplyTemplate { get; set; }

    public virtual ICollection<Template> InverseAutoReplyTemplate { get; set; } = [];

    public virtual Project Project { get; set; } = null!;

    public virtual Service? Service { get; set; }

    public virtual ICollection<Submission> Submissions { get; set; } = [];

    public virtual ICollection<TemplateAttachment> TemplateAttachments { get; set; } = [];
}
