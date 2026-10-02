using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emaily.BLL.DTOs.Admin
{
    public class SystemLogDto
    {
        public long Id { get; set; }
        public string LogLevel { get; set; } = null!;
        public string ExecutionTrace { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string? ExceptionDetails { get; set; }
        public string? UserId { get; set; }
        public string? ProjectId { get; set; }
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}



