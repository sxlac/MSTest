using System.Diagnostics.CodeAnalysis;

namespace Signify.FOBT.Messages.Events.Status;

[ExcludeFromCodeCoverage]
public class BillRequestNotSent : BaseStatusMessage
{
    public string BillingProductCode { get; set; }
}