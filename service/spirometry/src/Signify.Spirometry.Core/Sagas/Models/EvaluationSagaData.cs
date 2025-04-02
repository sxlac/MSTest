using NServiceBus;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.Spirometry.Core.Sagas.Models;

[ExcludeFromCodeCoverage]
public class EvaluationSagaData : ContainSagaData
{
    /// <remarks>
    /// Identifier of this saga
    /// </remarks>
    public long EvaluationId { get; set; }

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
    #endregion Finalized

    #region Overread
    /// <summary>
    /// Whether or not this evaluation needs an overread
    /// </summary>
    /// <remarks>
    /// <c>null</c> until an EvaluationFinalizedEvent is received
    /// </remarks>
    public bool? NeedsOverread { get; set; }

    /// <summary>
    /// PK of the OverreadResult in db
    /// </summary>
    /// <remarks>
    /// <c>null</c> until an overread is received from the webhook
    /// </remarks>
    public int? OverreadResultId { get; set; }

    /// <summary>
    /// UTC timestamp when an overread was received
    /// </summary>
    public DateTime? OverreadReceivedDateTime { get; set; }

    /// <summary>
    /// UTC timestamp when this saga raised a ProcessOverread command
    /// </summary>
    public DateTime? ProcessOverreadCommandSentDateTime { get; set; }

    /// <summary>
    /// UTC timestamp when processing a spirometry overread completed
    /// </summary>
    public DateTime? OverreadProcessedDateTime { get; set; }
    #endregion Overread

    #region Flag
    /// <summary>
    /// Whether or not this evaluation needs a flag for a clarification to be sent to the provider
    /// </summary>
    /// <remarks>
    /// <c>null</c> until an EvaluationFinalizedEvent is received
    /// </remarks>
    public bool? NeedsFlag { get; set; }

    /// <summary>
    /// PK of the ClarificationFlag in db
    /// </summary>
    public int? ClarificationFlagId { get; set; }

    /// <summary>
    /// UTC timestamp when a flag was created for this spirometry exam
    /// </summary>
    public DateTime? FlagCreatedDateTime { get; set; }

    /// <summary>
    /// UTC timestamp when this saga raised a CreateFlag command
    /// </summary>
    public DateTime? CreateFlagCommandSentDateTime { get; set; }
    #endregion Flag

    #region Hold
    /// <summary>
    /// PK of the Hold in db
    /// </summary>
    /// <remarks>
    /// <c>null</c> until a HoldCreated event is received.
    ///
    /// Not to be mistaken for the hold's identifier outside of the Spirometry context, which
    /// is the CdiHoldId.
    /// </remarks>
    public int? HoldId { get; set; }

    /// <summary>
    /// UTC timestamp when processing of the HoldCreated event completed
    /// </summary>
    public DateTime? HoldCreatedDateTime { get; set; }

    /// <summary>
    /// UTC timestamp when the CDI evaluation hold was released/expired
    /// </summary>
    public DateTime? HoldReleasedDateTime { get; set; }

    /// <summary>
    /// UTC timestamp when this saga raised a ReleaseHold command
    /// </summary>
    public DateTime? ReleaseHoldCommandSentDateTime { get; set; }
    #endregion Hold

    #region Billing
    /// <summary>
    /// PK of the pdfdelivery event in db
    /// </summary>
    public int? PdfDeliveredToClientId { get; set; }

    /// <summary>
    /// Whether or not this spirometry exam is billable
    /// </summary>
    /// <remarks>
    /// Not necessarily whether it is billable <i>now</i>, reference <see cref="PdfDeliveredToClientId"/> for that.
    ///
    /// <c>null</c> until it can definitively be determined that this exam may or may not be billed for.
    /// </remarks>
    public bool? IsBillable { get; set; }

    /// <summary>
    /// UTC timestamp when the pdfdelivery event was received
    /// </summary>
    public DateTime? PdfDeliveredToClientDateTime { get; set; }

    /// <summary>
    /// UTC timestamp when the CreateBillingRequest command was sent
    /// </summary>
    public DateTime? ProcessPdfDeliveryCommandSentDateTime { get; set; }

    /// <summary>
    /// UTC timestamp of when the pdfdelivery event was processed
    /// </summary>
    public DateTime? PdfDeliveryProcessedDateTime { get; set; }
    #endregion Billing
}