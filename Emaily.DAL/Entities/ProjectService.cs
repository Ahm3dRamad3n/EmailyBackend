using System;
using System.Collections.Generic;

namespace Emaily.DAL.Entities;

public partial class ProjectService
{
    public int ClusterKey { get; set; }

    public string ProjectId { get; set; } = null!;

    public string ServiceId { get; set; } = null!;

    public DateTime AssignedAt { get; set; }

    public virtual Project Project { get; set; } = null!;

    public virtual Service Service { get; set; } = null!;
}
