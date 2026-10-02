using System;
using System.Collections.Generic;

namespace Emaily.DAL.Entities;

public partial class Submission
{ 
    public int ClusterKey { get; set; }

    public Guid Id { get; set; }

    public string ProjectId { get; set; } = null!; 

    public string TemplateId { get; set; } = null!;

    public string RecipientEmail { get; set; } = null!;

    public string? RecipientName { get; set; }

    public string Subject { get; set; } = null!;

    public string PayloadJson { get; set; } = null!;

    public string RawHtmlBody { get; set; } = null!;

    public DateTime ReceivedAt { get; set; }

    public string Status { get; set; } = null!; // CHECK (Status IN ('Sent', 'Failed', 'Pending', 'Resent', 'Discarded', 'QuotaExceeded'))

    public bool IsPrivateData { get; set; }

    public string? AiSummary { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime? SentAt { get; set; }

    public virtual Project Project { get; set; } = null!;

    public virtual Template? Template { get; set; }
}
