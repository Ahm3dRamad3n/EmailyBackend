using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emaily.BLL.DTOs.Submission
{
    public class SubmissionDetailsDto : SubmissionHistoryDto
    {
        public string TemplateId { get; set; } = null!;
        public string PayloadJson { get; set; } = null!;
        public string RawHtmlBody { get; set; } = null!;
        public string? ErrorMessage { get; set; }
        public string? AiSummary { get; set; }
    }
}
