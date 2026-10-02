using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emaily.BLL.DTOs.Admin
{
    public class DashboardStatsDto
    {
        public int TotalUsers { get; set; }
        public decimal TotalRevenue { get; set; }
        public int PendingSubmissions { get; set; }
        public int TotalProjects { get; set; }
        public int FailedSubmissions { get; set; }
        public int ActiveUsers { get; set; }
    }
}
