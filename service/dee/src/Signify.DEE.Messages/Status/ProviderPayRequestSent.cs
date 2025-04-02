using System;

namespace Signify.DEE.Messages.Status;

public class ProviderPayRequestSent : BaseStatusMessage
{
    public string ProviderPayProductCode { get; set; }
    public string PaymentId { get; set; }
    public DateTimeOffset ParentEventDateTime { get; set; }
}