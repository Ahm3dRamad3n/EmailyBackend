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
    public class AdminSubscribeRequestDto
    {
        [NotEmptyGuid]
        public Guid PlanId { get; set; }

        [NotEmptyGuid]
        public Guid UserId { get; set; } 
    }
}
