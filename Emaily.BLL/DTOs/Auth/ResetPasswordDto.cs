using System;
using System.Collections.Generic;
using System.Linq;
using Emaily.BLL.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;

namespace Emaily.BLL.DTOs.Auth
{
    public class ResetPasswordDto
    {
        [RequiredText]
        public string Token { get; set; } = null!;
        [StrongPassword]
        public string NewPassword { get; set; } = null!;
    }
}
