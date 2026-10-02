using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Emaily.BLL.DTOs
{
    // string message, string Source = null, Guid? userId = null, string projectId = null, string ExceptionDetails = null
    public class Log(Guid userId, string message, string? projectId = null, string? exceptionDetails = null)
    {
        public string Message { get; set; } = message;
        public Guid UserId { get; set; } = userId;
        public string? ProjectId { get; set; } = projectId;
        public string? ExceptionDetails { get; set; } = exceptionDetails;
    }
}
