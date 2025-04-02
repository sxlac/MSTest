using System;

namespace Signify.eGFR.Core.Events.Status;

public class BillRequestNotSent : BaseStatusMessage
{
    public string BillingProductCode { get; set; } 

    public DateTimeOffset PdfDeliveryDate { get; set; }
}