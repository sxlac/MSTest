using System;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace SpiroNsb.SagaEvents
{
    /// <summary>
    /// Event triggered when an evaluation has been held in CDI
    /// </summary>
    public class HoldCreatedEvent : ISagaEvent
    {
        /// <inheritdoc />
        public long EvaluationId { get; set; }

        /// <inheritdoc />
        public DateTime CreatedDateTime { get; set; }

        /// <summary>
        /// Identifier of this hold in the Spirometry database
        /// </summary>
        /// <remarks>
        /// Not to be mistaken for the hold's identifier outside of the Spirometry context, which
        /// is the CdiHoldId
        /// </remarks>
        public int HoldId { get; set; }

        public HoldCreatedEvent(long evaluationId, DateTime createdDateTime, int holdId)
        {
            EvaluationId = evaluationId;
            CreatedDateTime = createdDateTime;
            HoldId = holdId;
        }
    }
}
