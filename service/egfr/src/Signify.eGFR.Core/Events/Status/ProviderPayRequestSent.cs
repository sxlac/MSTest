using System;

namespace Signify.eGFR.Core.Events.Status;

public class ProviderPayRequestSent : BaseStatusMessage
{
    // Making it static affects how the Kafka event is serialized and it will miss this field.
#pragma warning disable CA1822
    public string ProviderPayProductCode => Constants.ProductCodes.eGFR;
#pragma warning restore CA1822
    public string PaymentId { get; set; }

    /// <summary>
    /// Date and time contained within the parent kafka event that triggered this status change
    /// </summary>
    public DateTimeOffset ParentEventDateTime { get; set; }
}