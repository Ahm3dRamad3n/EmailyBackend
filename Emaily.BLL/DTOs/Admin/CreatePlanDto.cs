using Emaily.BLL.Attributes;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emaily.BLL.DTOs.Admin
{
    public class CreatePlanDto
    {
        [RequiredText] public string Name { get; set; } = null!;
        [PositiveNumber] public decimal MonthlyPrice { get; set; }
        [PositiveNumber] public int MaxEmailsPerMonth { get; set; }
        [PositiveNumber] public int MaxProjects { get; set; }
        [PositiveNumber] public int MaxServices { get; set; }
        [PositiveNumber] public int MaxTemplates { get; set; }
        [PositiveNumber] public int MaxAttachmentsPerTemplate { get; set; }
        public bool CanUseGoogleSheets { get; set; }
        public bool CanUseAI { get; set; }
        public bool CanUseTelegramBot { get; set; }
    }
}
