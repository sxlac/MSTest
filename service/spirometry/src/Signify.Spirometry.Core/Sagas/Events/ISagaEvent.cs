using System;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace SpiroNsb.SagaEvents
{
    /// <summary>
    /// Interface for all saga events
    /// </summary>
    public interface ISagaEvent
    {
        /// <summary>
        /// Identifier of the evaluation
        /// </summary>
        long EvaluationId { get; set; }

        /// <summary>
        /// UTC timestamp of when this event occurred
        /// </summary>
        DateTime CreatedDateTime { get; set; }
    }
}
