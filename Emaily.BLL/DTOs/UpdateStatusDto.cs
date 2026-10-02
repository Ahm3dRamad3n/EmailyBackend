using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emaily.BLL.DTOs
{
    public class UpdateStatusDto
    {
        [Required]
        public bool IsActive { get; set; }
    }
}
