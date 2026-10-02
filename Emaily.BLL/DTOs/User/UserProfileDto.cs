using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emaily.BLL.DTOs.User
{
    public class UserProfileDto
    {
        public string Id { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string DevNotificationEmail { get; set; } = null!;
        public int RemainingQuota { get; set; }
        public int OverageEmails { get; set; }
        public string Roles { get; set; } = null!;
    }
}
