using System;

namespace Signify.DEE.Messages.Status;

/// <summary>
/// Status event signifying that a billing request was not sent to RCM for the corresponding evaluation where a DEE exam was performed
/// </summary>
public class BillRequestNotSent : BaseStatusMessage
{
    public DateTimeOffset PdfDeliveryDate { get; set; }
    public string BillingProductCode { get; set; }
}