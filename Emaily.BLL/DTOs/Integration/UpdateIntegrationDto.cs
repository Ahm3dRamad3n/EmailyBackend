using System;
using Emaily.BLL.Attributes;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Emaily.BLL.DTOs.Integration
{
    public class UpdateIntegrationDto
    {
        [RequiredText]
        public string ConfigJson { get; set; } = null!;
    }
}
