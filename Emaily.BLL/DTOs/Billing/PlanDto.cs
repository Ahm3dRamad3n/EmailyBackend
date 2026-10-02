using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emaily.BLL.DTOs.Billing
{
    public class PlanDto
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public decimal MonthlyPrice { get; set; }
        public int MaxEmailsPerMonth { get; set; }
        public int MaxProjects { get; set; }
        public int MaxServices { get; set; }
        public int MaxTemplates { get; set; }
        public int MaxAttachmentsPerTemplate { get; set; }
        public bool CanUseGoogleSheets { get; set; }
        public bool CanUseAI { get; set; }
        public bool CanUseTelegramBot { get; set; }
    }
}
