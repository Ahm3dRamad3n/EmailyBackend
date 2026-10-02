using System;
using System.Collections.Generic;

namespace Emaily.DAL.Entities;

public partial class TemplateAttachment
{
    public int ClusterKey { get; set; }

    public Guid Id { get; set; }

    public string TemplateId { get; set; } = null!;

    public string FileName { get; set; } = null!;

    public string FileUrl { get; set; } = null!;

    public long FileSizeInBytes { get; set; }

    public virtual Template Template { get; set; } = null!;
}
