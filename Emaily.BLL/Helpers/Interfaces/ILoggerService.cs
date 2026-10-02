using Emaily.BLL.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Emaily.BLL.Helpers.Interfaces
{
    public interface ILoggerService
    {
        void LogDebug(Log dto, 
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0);
        void LogInfo(Log dto, 
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0);
        void LogWarning(Log dto, 
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0);
        void LogError(Log dto, 
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0);
        void LogCritical(Log dto, 
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0);
    }
}
