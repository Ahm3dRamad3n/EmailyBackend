using System;
using System.Collections.Generic;

namespace Emaily.DAL.Entities;

public partial class Invoice
{
    public int ClusterKey { get; set; }

    public Guid Id { get; set; }

    public Guid SubscriptionId { get; set; }

    public decimal Amount { get; set; }

    public string Status { get; set; } = null!; // CHECK (Status IN ('Pending', 'Paid', 'Failed', 'Refunded', 'Void'))

    public DateTime InvoiceDate { get; set; }

    public string InvoicePdfUrl { get; set; } = null!;

    public virtual Subscription Subscription { get; set; } = null!;
}
