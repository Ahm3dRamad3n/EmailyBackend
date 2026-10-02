using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emaily.BLL.DTOs.Billing
{
    public class SubscriptionDetailsDto
    {
        public string UserId { get; set; } = null!;
        public string SubscriptionId { get; set; } = null!;
        public string PlanId { get; set; } = null!;
        public string PlanName { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // حالة الاستهلاك الفعلي
        public int RemainingEmails { get; set; }
        public int OverageEmails { get; set; }

        public int UsedProjects { get; set; }
        public int MaxProjects { get; set; }

        public int UsedServices { get; set; }
        public int MaxServices { get; set; }

        public int UsedTemplates { get; set; }
        public int MaxTemplates { get; set; }
    }
}
