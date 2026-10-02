using System;
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
        [Required]
        public JsonElement ConfigJson { get; set; }
    }
}
