using System;
using System.Collections.Generic;

namespace Emaily.DAL.Entities;

public partial class Service
{
    public struct ProviderTypes
    {
        public const string AppPassword = "AppPassword";
        public const string ApiKey = "ApiKey";
        public const string OAuth = "OAuth";
    }
}
