using Emaily.BLL.Attributes;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emaily.BLL.DTOs.Admin
{
    public class BanDto
    {
        [RequiredText]
        public string IpAddress { get; set; } = null!; // IP or UserEmail
        public string Reason { get; set; } = null!;
    }
}
