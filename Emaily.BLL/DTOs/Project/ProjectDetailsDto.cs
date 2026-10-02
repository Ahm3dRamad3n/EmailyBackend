using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emaily.BLL.DTOs.Project
{
    public class ProjectDetailsDto : ProjectDto
    {
        public string PublicApiKey { get; set; } = null!;
        public string PrivateApiKey { get; set; } = null!;
        public string? RestrictedDomains { get; set; }
        public string ProjectAccessMode { get; set; } = null!;
    }
}
