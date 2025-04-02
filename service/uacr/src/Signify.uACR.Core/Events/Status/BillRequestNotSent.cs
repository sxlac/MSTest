using System.Diagnostics.CodeAnalysis;
using System;

namespace Signify.uACR.Core.Events.Status;

[ExcludeFromCodeCoverage]
public class BillRequestNotSent : BaseStatusMessage
{
    public string BillingProductCode { get; set; } 

    public DateTimeOffset PdfDeliveryDate { get; set; }
}