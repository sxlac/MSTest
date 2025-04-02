using System;
using NServiceBus;

namespace Signify.A1C.Svc.Core.ApiClient.Requests
{
    public class UpdateInventoryRequest : ICommand
    {
        public Guid CorrelationId { get; set; }
        public Guid RequestId { get; set; }
        public int A1CId { get; set; }
        public string ItemNumber { get; set; }
        public DateTime DateUpdated { get; set; }
        public string SerialNumber { get; set; }
        public int Quantity { get; set; }
        public int ProviderId { get; set; }
        public string CustomerNumber { get; set; }
        public int EvaluationId { get; set; }

        public UpdateInventoryRequest()
        {
            Quantity = 1;
            RequestId = Guid.NewGuid();
        }
    }
}