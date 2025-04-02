using System;

namespace Signify.DEE.Messages.Status;

/// <summary>
/// Status event signifying that a billing request was sent to RCM for the corresponding evaluation where a DEE exam was performed
/// </summary>
public class BillRequestSent : BaseStatusMessage
{
    public string BillingProductCode { get; set; }
    public string BillId { get; set; }
    public DateTimeOffset PdfDeliveryDate { get; set; }
}