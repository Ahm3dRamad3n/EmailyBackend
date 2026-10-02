using Emaily.BLL.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emaily.BLL.DTOs.Billing
{
    public class SubscribeRequestDto
    {
        [NotEmptyGuid]
        public Guid PlanId { get; set; }

        public string? PaymentMethodId { get; set; } 
    }
}
