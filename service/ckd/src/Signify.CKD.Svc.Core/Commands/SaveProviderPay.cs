using System;
using NServiceBus;

namespace Signify.CKD.Svc.Core.Commands;

public class SaveProviderPay : ICommand
{
    public Guid EventId { get; set; }
    public long EvaluationId { get; set; }
    public string PaymentId { get; set; }
    public DateTime PdfDeliveryDateTime { get; set; }
}