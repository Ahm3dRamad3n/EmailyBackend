using System;
using System.Collections.Generic;

namespace Emaily.DAL.Entities;

public partial class Subscription
{
    public struct Statuses
    {
        public const string Active = "Active";
        public const string Revoke = "Revoke";
        public const string Expired = "Expired";
    }
}
