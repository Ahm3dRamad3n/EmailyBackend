using Emaily.BLL.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emaily.BLL.DTOs.Auth
{
    public class SendVerificationEmailDto
    {
        [ValidEmail]
        public string Email { get; set; } = null!;

        [RequiredText(["register", "account"])]
        public string Target { get; set; } = null!;
    }
}
