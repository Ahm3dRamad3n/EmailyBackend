using Emaily.BLL.Attributes;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Emaily.BLL.DTOs.Integration
{
    public class CreateIntegrationDto
    {
        [ValidIntegrationType]
        public string IntegrationType { get; set; } = null!;

        [Required]
        // مثال لتليجرام: {"ChatId": "123456789"}
        // مثال لجوجل شيتس: {"sheetUrl": "https://docs.google.com/spreadsheets/d/1abc1234567890/edit#gid=0"}
        // نثال لتلخيص الذكاء الاصطناعي: [null now]
        public JsonElement ConfigJson { get; set; }
    }
}
