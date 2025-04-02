using NServiceBus;

namespace Signify.DEE.Svc.Core.Commands
{
    public class ProcessPdfDelivered : ICommand
    {
        public string EventId { get; set; }
        public long EvaluationId { get; set; }
        public DateTime DeliveryDateTime { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public long BatchId { get; set; }
        public string BatchName { get; set; }

        public ProcessPdfDelivered(string eventId, int evaluationId, DateTime deliveryDateTime, DateTime createdDateTime, long batchId, string batchName)
        {
            EventId = eventId;
            EvaluationId = evaluationId;
            DeliveryDateTime = deliveryDateTime;
            CreatedDateTime = createdDateTime;
            BatchId = batchId;
            BatchName = batchName;
        }

        public ProcessPdfDelivered()
        {

        }

        protected bool Equals(ProcessPdfDelivered other)
        {
            return EventId.Equals(other.EventId) && EvaluationId == other.EvaluationId && DeliveryDateTime == other.DeliveryDateTime && CreatedDateTime == other.CreatedDateTime && BatchId == other.BatchId && BatchName == other.BatchName;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ProcessPdfDelivered)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = EventId.GetHashCode();
                hashCode = (hashCode * 397) ^ EvaluationId.GetHashCode();
                hashCode = (hashCode * 397) ^ DeliveryDateTime.GetHashCode();
                hashCode = (hashCode * 397) ^ CreatedDateTime.GetHashCode();
                hashCode = (hashCode * 397) ^ BatchId.GetHashCode();
                hashCode = (hashCode * 397) ^ BatchName.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"{nameof(EventId)}: {EventId}, {nameof(EvaluationId)}: {EvaluationId}, {nameof(DeliveryDateTime)}: {DeliveryDateTime}, {nameof(CreatedDateTime)}: {CreatedDateTime}, {nameof(BatchId)}: {BatchId}, {nameof(BatchName)}: {BatchName}";
        }
    }
}