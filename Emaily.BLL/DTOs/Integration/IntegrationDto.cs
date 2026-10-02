using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emaily.BLL.DTOs.Integration
{
    public class IntegrationDto
    {
        public string Id { get; set; } = null!;
        public string ProjectId { get; set; } = null!;
        public string IntegrationType { get; set; } = null!;
        public string ConfigJson { get; set; } = null!;
        public bool IsActive { get; set; }
    }
}
