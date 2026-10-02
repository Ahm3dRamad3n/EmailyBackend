using Emaily.BLL.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emaily.BLL.DTOs.User
{
    public class ChangePasswordDto
    {
        [RequiredText]
        public string CurrentPassword { get; set; } = null!;

        [StrongPassword]
        public string NewPassword { get; set; } = null!;
    }
}
