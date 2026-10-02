using System;
using System.Collections.Generic;

namespace Emaily.DAL.Entities;

public partial class BanDetail
{
    public struct ActionTypes
    {
        public const string Violation = "Violation";
        public const string AutoBan = "AutoBan";
        public const string ManualBan = "ManualBan";
        public const string Pardon = "Pardon";
        public const string LevelReduced = "LevelReduced";
    }
    public struct BanViolationWeights
    {
        public const short SubmitSpam = 1;
        public const short LoginBruteForce = 3;
        public const short UnauthorizedAccess = 5;
    }
}


