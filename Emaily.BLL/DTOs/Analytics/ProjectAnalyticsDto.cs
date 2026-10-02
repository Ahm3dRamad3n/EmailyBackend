using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emaily.BLL.DTOs.Analytics
{
    public class ProjectAnalyticsDto
    {
        public string ProjectName { get; set; } = null!;
        public int Total { get; set; }
        public int TotalSent { get; set; }
        public int TotalFailed { get; set; }
        public double SuccessRate { get; set; }

        // بيانات الرسم البياني الخاصة بالمشروع
        public IEnumerable<VolumeByDayDto> VolumeByDay { get; set; } = [];

        // تفاصيل القوالب (Templates) التابعة لهذا المشروع
        public IEnumerable<TemplateSummaryDto> ByTemplate { get; set; } = [];
    }

    public class TemplateSummaryDto
    {
        public string TemplateName { get; set; } = null!;
        public int Total { get; set; }
        public int Sent { get; set; }
        public int Failed { get; set; }
        public double SuccessRate { get; set; }
    }
}
