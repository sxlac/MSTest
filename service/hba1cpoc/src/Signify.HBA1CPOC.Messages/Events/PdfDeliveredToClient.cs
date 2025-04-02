using System;
using System.Collections.Generic;
using NServiceBus;

namespace Signify.HBA1CPOC.Messages.Events
{
    public class PdfDeliveredToClient : IMessage
    {
        public Guid EventId { get; set; }
        public long EvaluationId { get; set; }
        public List<string> ProductCodes { get; set; }
        public DateTime DeliveryDateTime { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public long BatchId { get; set; }
        public string BatchName { get; set; }
    }
}
