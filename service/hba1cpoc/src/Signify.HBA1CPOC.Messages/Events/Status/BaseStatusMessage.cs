using System;

namespace Signify.HBA1CPOC.Messages.Events.Status
{
    /// <summary>
    /// Base class for all status events published to Kafka
    /// </summary>
    public abstract class BaseStatusMessage
    {
        public string ProductCode { get; set; }
        public int EvaluationId { get; set; }
        public long MemberPlanId { get; set; }
        public int ProviderId { get; set; }
        public DateTimeOffset CreateDate { get; set; }
        public DateTimeOffset ReceivedDate { get; set; }
    }
}
