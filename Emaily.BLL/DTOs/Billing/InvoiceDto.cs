using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emaily.BLL.DTOs.Billing
{
    public class InvoiceDto
    {
        public string Id { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Status { get; set; } = null!;
        public DateTime InvoiceDate { get; set; }
        public string InvoicePdfUrl { get; set; } = null!;
    }
}
