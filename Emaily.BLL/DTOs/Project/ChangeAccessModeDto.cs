using Emaily.BLL.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emaily.BLL.DTOs.Project
{
    public class ChangeAccessModeDto
    {
        [ValidAccessMode]
        public string AccessMode { get; set; } = null!;
    }
}
