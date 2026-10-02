using System;
using System.Collections.Generic;

namespace Emaily.DAL.Entities;

public partial class Subscription
{
    public int ClusterKey { get; set; }

    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid PlanId { get; set; }

    public string Status { get; set; } = null!; // CHECK(Status IN ('Active', 'Revoke', 'Expired'))

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public DateTime? RevokeAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Invoice> Invoices { get; set; } = [];

    public virtual Plan Plan { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
