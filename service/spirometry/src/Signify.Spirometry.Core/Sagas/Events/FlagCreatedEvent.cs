using System;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace SpiroNsb.SagaEvents
{
    /// <summary>
    /// Event triggerred when a flag has been raised to CDI for a provider clarification
    /// </summary>
    public class FlagCreatedEvent : ISagaEvent
    {
        /// <inheritdoc />
        public long EvaluationId { get; set; }

        /// <inheritdoc />
        public DateTime CreatedDateTime { get; set; }

        /// <summary>
        /// PK of the ClarificationFlag in db
        /// </summary>
        public int ClarificationFlagId { get; set; }

        public FlagCreatedEvent(long evaluationId, DateTime createdDateTime, int clarificationFlagId)
        {
            EvaluationId = evaluationId;
            CreatedDateTime = createdDateTime;
            ClarificationFlagId = clarificationFlagId;
        }
    }
}
