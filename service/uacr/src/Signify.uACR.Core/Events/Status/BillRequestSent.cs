using System.Diagnostics.CodeAnalysis;
using System;

namespace Signify.uACR.Core.Events.Status;

[ExcludeFromCodeCoverage]
public class BillRequestSent : BaseStatusMessage
{
    public string BillingProductCode { get; set; } 

    public Guid BillId { get; set; }

    public DateTimeOffset PdfDeliveryDate { get; set; }
}