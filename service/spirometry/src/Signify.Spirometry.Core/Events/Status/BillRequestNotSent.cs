using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.Spirometry.Core.Events.Status;

/// <summary>
/// Status event signifying that a billing request was not sent to RCM for the corresponding evaluation where a Spiro exam was performed
/// </summary>
[ExcludeFromCodeCoverage]
public class BillRequestNotSent : BaseStatusMessage
{
    public DateTime PdfDeliveryDate { get; set; }
}