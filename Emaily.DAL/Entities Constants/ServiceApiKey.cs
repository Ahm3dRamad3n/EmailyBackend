using System;
using System.Collections.Generic;

namespace Emaily.DAL.Entities;

public partial class ServiceApiKey
{
    public struct ProviderNames
    {
        public const string SendGrid = "SendGrid";
        public const string Resend = "Resend";
    }
}
