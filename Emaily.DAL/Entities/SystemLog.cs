using System;
using System.Collections.Generic;

namespace Emaily.DAL.Entities;

public partial class SystemLog
{
    public long Id { get; set; }

    public string LogLevel { get; set; } = null!; // CHECK(LogLevel IN ('Debug', 'Info', 'Warning', 'Error', 'Critical'))

    public string ExecutionTrace { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string? ExceptionDetails { get; set; }

    public Guid? UserId { get; set; }

    public string? ProjectId { get; set; }

    public string? IpAddress { get; set; }

    public DateTime CreatedAt { get; set; }
}
