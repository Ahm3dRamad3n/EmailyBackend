using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emaily.BLL.DTOs
{
    public class BotNotificationPayload
    {
        public string ChatId { get; set; } = null!;
        public string Text { get; set; } = null!;
        public string ParseMode { get; set; } = "HTML";
        public Guid? SubmissionId { get; set; }
    }
}
