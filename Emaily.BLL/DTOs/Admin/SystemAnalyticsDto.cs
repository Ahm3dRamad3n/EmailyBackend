using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emaily.BLL.DTOs.Admin
{
    public class AdminAnalyticsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int SuspendedUsers { get; set; }
        public int TotalProjects { get; set; }

        public int TotalEmailsSent { get; set; }
        public int TotalEmailsFailed { get; set; }
        public double OverallSuccessRate { get; set; }

        public decimal TotalMRR { get; set; }

        public List<RevenueByPlanDto> RevenueByPlan { get; set; } = [];
        public List<VolumeByDayDto> VolumeByDay { get; set; } = [];
        public SystemHealthDto SystemHealth { get; set; } = new();
    }

    public class RevenueByPlanDto
    {
        public string PlanName { get; set; } = null!;   
        public int SubscriberCount { get; set; }
        public decimal MRR { get; set; }
    }

    public class VolumeByDayDto
    {
        public string Date { get; set; } = null!;
        public int Sent { get; set; }
        public int Failed { get; set; }
        public int Pending { get; set; }
    }

    public class SystemHealthDto
    {
        public string Status { get; set; } = null!;
        public int QueueDepth { get; set; }
        public int AvgDeliveryTimeMs { get; set; }
    }
}
