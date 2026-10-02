using System;
using System.Collections.Generic;

namespace Emaily.DAL.Entities;

public partial class Project
{
    public int ClusterKey { get; set; }

    public string Id { get; set; } = null!;

    public Guid UserId { get; set; }

    public string Name { get; set; } = null!;

    public string PublicApiKey { get; set; } = null!;

    public string PrivateApiKey { get; set; } = null!;

    public string? RestrictedDomains { get; set; }

    public string ProjectAccessMode { get; set; } = null!; // CHECK(ProjectAccessMode IN ('FrontendOnly', 'BackendOnly', 'Hybrid'))

    public DateTime CreatedAt { get; set; }

    public bool IsActive { get; set; }

    public bool IsDeleted { get; set; }

    public bool IsLocked { get; set; }

    public virtual ICollection<Integration> Integrations { get; set; } = [];

    public virtual ICollection<ProjectService> ProjectServices { get; set; } =  [];

    public virtual ICollection<Submission> Submissions { get; set; } =  [];

    public virtual ICollection<Template> Templates { get; set; } = [];

    public virtual User User { get; set; } = null!;
}
