using System;
using System.Collections.Generic;

namespace Emaily.DAL.Entities;

public partial class Integration
{
    public struct IntegrationTypes
    {
        public const string TelegramBot = "TelegramBot";
        public const string GoogleSheets = "GoogleSheets";
        public const string AiSummary = "AiSummary";
    }
}
