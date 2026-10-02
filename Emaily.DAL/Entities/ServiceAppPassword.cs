using System;
using System.Collections.Generic;

namespace Emaily.DAL.Entities;

public partial class ServiceAppPassword
{
    public int ClusterKey { get; set; }

    public string ServiceId { get; set; } = null!;

    public string SmtpHost { get; set; } = null!;

    public int SmtpPort { get; set; }

    public string Username { get; set; } = null!;

    public string EncryptedPassword { get; set; } = null!;

    public virtual Service Service { get; set; } = null!;
}
