using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emaily.BLL.DTOs.Admin
{
    public class AdminUserDetailsDto : AdminUserDto
    {
        public int RemainingQuota { get; set; }
        public int OverageEmails { get; set; }
        public string DevNotificationEmail { get; set; } = null!;
        public string PlanName { get; set; } = null!;
        public int ProjectsCount { get; set; }
        public int ServicesCount { get; set; }
        public int TemplatesCount { get; set; }
        public string Roles { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
