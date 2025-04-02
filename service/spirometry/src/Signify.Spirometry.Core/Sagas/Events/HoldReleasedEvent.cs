using System;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace SpiroNsb.SagaEvents
{
    /// <summary>
    /// Event triggerred when an evaluation hold in CDI has been released or expired
    /// </summary>
    public class HoldReleasedEvent : ISagaEvent
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

        public HoldReleasedEvent(long evaluationId, DateTime createdDateTime, int holdId)
        {
            EvaluationId = evaluationId;
            CreatedDateTime = createdDateTime;
            HoldId = holdId;
        }
    }
}
