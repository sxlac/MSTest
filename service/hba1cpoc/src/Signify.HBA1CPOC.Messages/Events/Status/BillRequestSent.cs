using System;

namespace Signify.HBA1CPOC.Messages.Events.Status
{
    public class BillRequestSent : BaseStatusMessage
    {
        public string BillingProductCode { get; set; }
        public string BillId { get; set; }
        public DateTime PdfDeliveryDate { get; set; }
    }
}