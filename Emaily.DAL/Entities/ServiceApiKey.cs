using System;
using System.Collections.Generic;

namespace Emaily.DAL.Entities;

public partial class ServiceApiKey
{
    public int ClusterKey { get; set; }

    public string ServiceId { get; set; } = null!;

    public string ProviderName { get; set; } = null!; // e.g., "SendGrid", "Resend"

    public string SecretApiKey { get; set; } = null!;

    public virtual Service Service { get; set; } = null!;
}
