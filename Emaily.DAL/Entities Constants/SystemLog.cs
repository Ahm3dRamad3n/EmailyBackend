using System;
using System.Collections.Generic;

namespace Emaily.DAL.Entities;

public partial class SystemLog
{
    public struct LogLevels
    {
        public const string Debug = "Debug";
        public const string Info = "Info";
        public const string Warning = "Warning";
        public const string Error = "Error";
        public const string Critical = "Critical";
    }
}
