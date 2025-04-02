using System;
using NServiceBus;

namespace Signify.HBA1CPOC.Svc.Core.Commands;

public class SaveProviderPay : ICommand
{
    public Guid EventId { get; set; }
    public int EvaluationId { get; set; }
    public string PaymentId { get; set; }
    public DateTime PdfDeliveryDateTime { get; set; }
}