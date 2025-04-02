using NServiceBus;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace SpiroNsb.SagaCommands
{
    /// <summary>
    /// Interface for all saga commands
    /// </summary>
    public interface ISagaCommand : ICommand
    {
        /// <summary>
        /// Identifier of the evaluation
        /// </summary>
        long EvaluationId { get; set; }
    }
}
