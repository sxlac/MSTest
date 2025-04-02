using System;

namespace Signify.A1C.Core.Events
{
    public sealed class BarcodeUpdate
    {
        public int? MemberPlanId { get; set; }
        public int? EvaluationId { get; set; }
        public string ProductCode { get; set; }
        public string Barcode { get; set; }
        public Guid? OrderCorrelationId { get; set; }
    }
}