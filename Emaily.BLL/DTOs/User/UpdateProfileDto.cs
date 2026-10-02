using System;
using System.Collections.Generic;
using Emaily.BLL.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emaily.BLL.DTOs.User
{
    public class UpdateProfileDto
    {
        [RequiredText, StringLength(150, MinimumLength = 3)]
        public string FullName { get; set; } = null!;

        [ValidEmail, StringLength(255)]
        public string DevNotificationEmail { get; set; } = null!;
    }
}
