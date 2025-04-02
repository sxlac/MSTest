using System;
using System.ComponentModel.DataAnnotations;

namespace Signify.CKD.Svc.Core.Data.Entities
{
    public class PDFToClient
    {
        [Key]
        public int PDFDeliverId { get; set; }
        public string EventId { get; set; }
        public long EvaluationId { get; set; }
        public DateTime DeliveryDateTime { get; set; }
        public DateTime DeliveryCreatedDateTime { get; set; }
        public long BatchId { get; set; }
        public string BatchName { get; set; }
        public int CKDId { get; set; }
    }
}
