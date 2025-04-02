using System;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace SpiroNsb.SagaEvents;

/// <summary>
/// Event triggerred when a spirometry diagnostic overread has been processed
/// </summary>
[ExcludeFromCodeCoverage]
public class OverreadProcessedEvent : ISagaEvent
{
    /// <inheritdoc />
    public long EvaluationId { get; set; }

    /// <inheritdoc />
    public DateTime CreatedDateTime { get; set; }

    /// <summary>
    /// Whether a flag should be created in CDI for clarification to the provider
    /// </summary>
    public bool NeedsFlag { get; set; }

    /// <summary>
    /// Whether the spirometry exam is billable according to the overread results
    /// </summary>
    public bool IsBillable { get; set; }
}