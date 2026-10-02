using System;
using System.Collections.Generic;

namespace Emaily.DAL.Entities;

public partial class Project
{
    public struct AccessModes
    {
        public const string FrontendOnly = "FrontendOnly";
        public const string BackendOnly = "BackendOnly";
        public const string Hybrid = "Hybrid";
    }
}
