using System;

namespace Signify.HBA1CPOC.Messages.Events.Status;

public class ProviderPayRequestSent : BaseStatusMessage
{
    public string ProviderPayProductCode { get; set; }
    public string PaymentId { get; set; }
    public DateTime PdfDeliveryDate { get; set; }
}