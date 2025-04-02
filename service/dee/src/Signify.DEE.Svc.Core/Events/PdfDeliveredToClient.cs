using NServiceBus;
using System;
using System.Collections.Generic;

namespace Signify.DEE.Svc.Core.Events;

public class PdfDeliveredToClient : IMessage
{
    public string EventId { get; set; }
    public long EvaluationId { get; set; }
    public List<string> ProductCodes { get; set; }
    public DateTimeOffset DeliveryDateTime { get; set; }
    public DateTimeOffset CreatedDateTime { get; set; }
    public long BatchId { get; set; }
    public string BatchName { get; set; }
}