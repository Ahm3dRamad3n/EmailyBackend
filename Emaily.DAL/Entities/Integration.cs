using System;
using System.Collections.Generic;

namespace Emaily.DAL.Entities;

public partial class Integration
{
    public int ClusterKey { get; set; }

    public Guid Id { get; set; }

    public string ProjectId { get; set; } = null!;

    public string IntegrationType { get; set; } = null!; // CHECK(IntegrationType IN ('TelegramBot', 'GoogleSheets', 'AiSummary')

    public string ConfigJson { get; set; } = null!;

    public bool IsActive { get; set; }

    public bool IsDeleted { get; set; }

    public virtual Project Project { get; set; } = null!;
}
