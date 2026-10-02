using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emaily.BLL.DTOs.Analytics
{
    public class OverviewAnalyticsDto
    {
        public int Total { get; set; }
        public int TotalSent { get; set; }
        public int TotalFailed { get; set; }
        public double SuccessRate { get; set; }

        // بيانات الرسم البياني
        public IEnumerable<VolumeByDayDto> VolumeByDay { get; set; } = [];

        // تفاصيل كل مشروع في الجدول السفلي
        public IEnumerable<ProjectSummaryDto> ByProject { get; set; } = [];
    }

    public class ProjectSummaryDto
    {
        public string ProjectId { get; set; } = null!;
        public string ProjectName { get; set; } = null!;
        public int Total { get; set; }
        public int Sent { get; set; }
        public int Failed { get; set; }
        public double SuccessRate { get; set; }
    }
}
