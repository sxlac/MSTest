using System;
using System.Diagnostics.CodeAnalysis;
using NServiceBus;

namespace Signify.Spirometry.Core.Sagas.Models;

[ExcludeFromCodeCoverage]
public class ProviderPaySagaData : ContainSagaData
{
    /// <remarks>
    /// Identifier of this saga
    /// </remarks>
    public long EvaluationId { get; set; }

    #region Overread

    /// <summary>
    /// Whether or not this evaluation needs an overread
    /// </summary>
    /// <remarks>
    /// <c>null</c> until an EvaluationFinalizedEvent is received
    /// </remarks>
    public bool? NeedsOverread { get; set; }

    /// <summary>
    /// UTC timestamp when processing a spirometry overread completed
    /// </summary>
    public DateTime? OverreadProcessedDateTime { get; set; }

    #endregion

    #region Finalized

    /// <summary>
    /// Whether or not the spirometry test was performed
    /// </summary>
    /// <remarks>
    /// <c>null</c> until an EvaluationFinalizedEvent is received
    /// </remarks>
    public bool? IsPerformed { get; set; }

    /// <summary>
    /// PK of the SpirometryExam in db
    /// </summary>
    /// <remarks>
    /// <c>null</c> until an EvaluationFinalizedEvent is received
    /// </remarks>
    public int? SpirometryExamId { get; set; }

    /// <summary>
    /// UTC timestamp when processing the EvaluationFinalizedEvent completed 
    /// </summary>
    public DateTime? FinalizedProcessedDateTime { get; set; }

    #endregion

    #region Payment

    /// <summary>
    /// Whether a valid cdi event was received that satisfied business rules
    /// and payment was done once already
    /// </summary>
    /// <remarks>
    /// Changes from false to true when payment is complete 
    /// </remarks>
    public bool IsPaymentComplete { get; set; }

    /// <summary>
    /// Whether or not this spirometry exam is payable
    /// </summary>
    /// <remarks>
    /// Not necessarily whether it is payable <i>now</i>, the final decision is made within ProcessPayment Saga command
    ///
    /// <c>null</c> until it can definitively be determined that this exam may or may not be paid for.
    /// </remarks>
    public bool? IsPayable { get; set; }
    
    /// <summary>
    /// Whether or not a cdi event for payment has been received
    /// </summary>
    /// <remarks>
    /// <c>null</c> until a cdi event has been received. This gets overwritten each time a new CdiEvent is received.
    /// </remarks>
    public DateTimeOffset? CdiEventReceivedDateTime { get; set; }

    #endregion
}