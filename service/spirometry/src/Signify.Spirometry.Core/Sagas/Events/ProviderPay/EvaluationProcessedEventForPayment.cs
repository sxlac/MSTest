using System;
using System.Diagnostics.CodeAnalysis;
#pragma warning disable S1128 // SonarQube: Remove this unnecessary 'using' - SQ isn't smart enough to realize it's needed for the cref in the xml comment below
using SpiroNsb.Sagas;
#pragma warning restore S1128

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace SpiroNsb.SagaEvents;

/// <summary>
/// Event triggered for handling <see cref="ProviderPaySaga"/> when processing an EvaluationFinalizedEvent has completed
/// </summary>
[ExcludeFromCodeCoverage]
public class EvaluationProcessedEventForPayment : ISagaEvent
{
    /// <inheritdoc />
    public long EvaluationId { get; set; }

    /// <inheritdoc />
    public DateTime CreatedDateTime { get; set; }

    /// <summary>
    /// PK of the SpirometryExam in db
    /// </summary>
    public int SpirometryExamId { get; set; }

    /// <summary>
    /// Whether or not a spirometry exam was performed for this evaluation
    /// </summary>
    public bool IsPerformed { get; set; }

    /// <summary>
    /// Whether or not a diagnostic overread is required for this evaluation's spirometry exam
    /// </summary>
    /// <remarks>
    /// We always know whether or not an overread is needed at time of POC:
    /// - Performed: SessionGrade IN (D/E/F)
    /// - Not Performed: false
    /// </remarks>
    public bool NeedsOverread { get; set; }

    /// <summary>
    /// Whether or not the exam results are payable
    /// </summary>
    /// <remarks>
    /// <c>null</c> if Performed, but unable to determine if it is payable until overread results are processed
    /// </remarks>
    public bool? IsPayable { get; set; }
}