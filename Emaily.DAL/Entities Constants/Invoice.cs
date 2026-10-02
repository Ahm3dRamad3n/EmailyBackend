using System;
using System.Collections.Generic;

namespace Emaily.DAL.Entities;

public partial class Invoice
{
    public struct Statuses
    {
        public const string pending = "Pending";
        public const string Paid = "Paid";
        public const string Failed = "Failed";
        public const string Refunded = "Refunded";
        public const string Void = "Void";
    }
}
