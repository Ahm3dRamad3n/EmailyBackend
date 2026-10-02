using System;
using System.Collections.Generic;

namespace Emaily.DAL.Entities;

public partial class Banned
{
    public int ClusterKey { get; set; }

    public string IpAddress { get; set; } = null!; // IP or UserEmail

    public int BanLevel { get; set; } // between 0 and 8

    public DateTime BannedUntil { get; set; }

    public virtual ICollection<BanDetail> BanDetails { get; set; } = [];
}
