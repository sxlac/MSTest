using System;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace SpiroNsb.SagaEvents
{
    /// <summary>
    /// Event triggerred when a diagnostic overread is received for a spirometry exam
    /// </summary>
    public class OverreadReceivedEvent : ISagaEvent
    {
        /// <inheritdoc />
        public long EvaluationId { get; set; }

        /// <inheritdoc />
        public DateTime CreatedDateTime { get; set; }

        /// <summary>
        /// PK of the OverreadResult in db
        /// </summary>
        public int OverreadResultId { get; set; }
    }
}
