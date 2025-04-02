using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.Events.Status;

/// <summary>
/// Status event signifying that a billing request was not sent to RCM for
/// the corresponding evaluation where a PAD exam was performed
/// </summary>
[ExcludeFromCodeCoverage]
public class BillRequestNotSent : BaseStatusMessage
{
    public DateTimeOffset PdfDeliveryDate { get; set; }
}