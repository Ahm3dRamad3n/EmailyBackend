using System;
using System.Collections.Generic;

namespace Emaily.DAL.Entities;

public partial class ServiceOauth
{
    public int ClusterKey { get; set; }

    public string ServiceId { get; set; } = null!;

    public string OauthProvider { get; set; } = null!; // e.g., "Google", "Microsoft"

    public string AccessToken { get; set; } = null!;

    public string RefreshToken { get; set; } = null!;

    public DateTime TokenExpiry { get; set; }

    public virtual Service Service { get; set; } = null!;
}
