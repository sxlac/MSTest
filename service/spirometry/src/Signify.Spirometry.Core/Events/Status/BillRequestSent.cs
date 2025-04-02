using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.Spirometry.Core.Events.Status;

/// <summary>
/// Status event signifying that a billing request was sent to RCM for the corresponding evaluation where a SPIRO exam was performed
/// </summary>
[ExcludeFromCodeCoverage]
public class BillRequestSent : BaseStatusMessage
{
    public string BillingProductCode { get; } = Constants.ProductCodes.Spirometry;
    public Guid BillId { get; set; }
    public DateTime PdfDeliveryDate { get; set; }
}