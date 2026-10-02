using System;
using System.Collections.Generic;

namespace Emaily.DAL.Entities;

public partial class User
{
    public int ClusterKey { get; set; }

    public Guid Id { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string DevNotificationEmail { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public int RemainingQuota { get; set; }

    public int OverageEmails { get; set; }

    public string Roles { get; set; } = null!;

    public bool IsActive { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<Project> Projects { get; set; } = [];

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = [];

    public virtual ICollection<Service> Services { get; set; } = [];

    public virtual ICollection<Subscription> Subscriptions { get; set; } = [];
}
