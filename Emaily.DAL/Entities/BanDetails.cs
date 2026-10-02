using System;
using System.Collections.Generic;

namespace Emaily.DAL.Entities;

public partial class BanDetail
{
    public int ClusterKey { get; set; }
    public string IpAddress { get; set; } = null!;
    public string ActionType { get; set; } = null!; 
    public int ViolationWeight { get; set; } 
    public string Reason { get; set; } = null!;
    public string UserAgent { get; set; } = null!;
    public string Endpoint { get; set; } = null!;
    public Guid? AdminId { get; set; } 
    public DateTime CreatedAt { get; set; }

    public virtual Banned Banned { get; set; } = null!;
}


