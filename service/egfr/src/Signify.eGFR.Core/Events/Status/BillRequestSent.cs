using System;

namespace Signify.eGFR.Core.Events.Status;

public class BillRequestSent : BaseStatusMessage
{
    public string BillingProductCode { get; set; } 

    public Guid BillId { get; set; }

    public DateTimeOffset PdfDeliveryDate { get; set; }
}