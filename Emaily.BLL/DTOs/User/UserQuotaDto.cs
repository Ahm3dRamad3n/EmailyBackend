using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emaily.BLL.DTOs.User
{
    public class UserQuotaDto
    {
        public int RemainingQuota { get; set; }
        public int OverageEmails { get; set; }
    }
}
