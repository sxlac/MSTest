using System;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace SpiroNsb.SagaEvents;

/// <summary>
/// Event triggered when processing an EvaluationFinalizedEvent has completed
/// </summary>
[ExcludeFromCodeCoverage]
public class EvaluationProcessedEvent : ISagaEvent
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
    /// Whether or not the exam results are billable
    /// </summary>
    /// <remarks>
    /// <c>null</c> if Performed, but unable to determine if it is billable until overread results are processed
    /// </remarks>
    public bool? IsBillable { get; set; }
    
    /// <summary>
    /// Whether a clarification flag will be required
    /// </summary>
    /// <remarks>
    /// Will only ever be <c>null</c> or <c>false</c>. Where a spirometry test was performed, most cases it will
    /// be <c>null</c> because it is unknown until we receive the overread, but in Scenario 2, we know there will
    /// not be a flag although we still need to process the overread.
    /// </remarks>
    public bool? NeedsFlag { get; set; }
}