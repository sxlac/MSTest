using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.FOBT.Messages.Events.Status;

[ExcludeFromCodeCoverage]
public class BillRequestSent : BaseStatusMessage
{
    public string BillingProductCode { get; set; }
    public string BillId { get; set; }
    public DateTime? PdfDeliveryDate { get; set; }
}