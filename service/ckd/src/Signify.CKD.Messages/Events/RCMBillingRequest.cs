using System;
using System.Collections.Generic;
using NServiceBus;

namespace Signify.CKD.Messages.Events
{
    public class RCMBillingRequest : IMessage
    {
        public int CKDId { get; set; }
        public int EvaluationId { get; set; }
        public int SharedClientId { get; set; }
        public int ClientId { get; set; }
        public int MemberPlanId { get; set; }
        public DateTime DateOfService { get; set; }
        public string UsStateOfService { get; set; }
        public int ProviderId { get; set; }
        public string RcmProductCode { get; set; }
        public string ApplicationId { get; set; }
        public DateTime BillableDate { get; set; }
        public DateTime PdfDeliveryDateTime { get; set; }
        public Dictionary<string, string> AdditionalDetails { get; set; }
        public string CorrelationId { get; set; }
    }
}
