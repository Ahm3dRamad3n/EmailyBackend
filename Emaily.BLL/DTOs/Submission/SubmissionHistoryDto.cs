using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emaily.BLL.DTOs.Submission
{
    public class SubmissionHistoryDto
    {
        public string Id { get; set; } = null!;
        public string RecipientEmail { get; set; } = null!;
        public string Subject { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime ReceivedAt { get; set; }
        public DateTime? SentAt { get; set; }
    }
}
