using System;
using System.Collections.Generic;

namespace Emaily.DAL.Entities;

public partial class Submission
{  
    public struct Statuses
    {
        public const string Sent = "Sent";
        public const string Failed = "Failed";
        public const string Pending = "Pending";
        public const string Resent = "Resent";
        public const string Discarded = "Discarded";
        public const string QuotaExceeded = "QuotaExceeded";
    }
}
