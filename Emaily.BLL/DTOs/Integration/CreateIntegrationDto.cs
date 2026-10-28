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

        [RequiredText]
        public string ConfigJson { get; set; } = null!;
    }
}
