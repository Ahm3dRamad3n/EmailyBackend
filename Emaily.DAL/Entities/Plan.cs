using System;
using System.Collections.Generic;

namespace Emaily.DAL.Entities;

public partial class Plan
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public decimal MonthlyPrice { get; set; }

    public int MaxEmailsPerMonth { get; set; }

    public int MaxProjects { get; set; }

    public int MaxServices { get; set; }

    public int MaxTemplates { get; set; }

    public int MaxAttachmentsPerTemplate { get; set; }

    public bool CanUseGoogleSheets { get; set; }

    public bool CanUseAi { get; set; }

    public bool CanUseTelegramBot { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Subscription> Subscriptions { get; set; } = [];
}
