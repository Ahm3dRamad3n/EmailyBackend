using System;
using System.Collections.Generic;

namespace Emaily.DAL.Entities;

public partial class Service
{
    public int ClusterKey { get; set; }

    public string Id { get; set; } = null!;

    public Guid UserId { get; set; }

    public string ProviderType { get; set; } = null!; // CHECK(ProviderType IN ('AppPassword', 'ApiKey', 'OAuth'))

    public string FromEmail { get; set; } = null!;

    public string FromName { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public bool IsActive { get; set; }

    public bool IsDeleted { get; set; }

    public bool IsLocked { get; set; }

    public virtual ICollection<ProjectService> ProjectServices { get; set; } = [];

    public virtual ServiceApiKey? ServiceApiKey { get; set; }

    public virtual ServiceAppPassword? ServiceAppPassword { get; set; }

    public virtual ServiceOauth? ServiceOauth { get; set; }

    public virtual ICollection<Template> Templates { get; set; } = [];

    public virtual User User { get; set; } = null!;
}
