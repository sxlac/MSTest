using System.Diagnostics.CodeAnalysis;
using Signify.FOBT.Svc.Core.Data.Entities;

namespace Signify.FOBT.Svc.Core.Models;

/// <summary>
/// Model for response from Evaluation API
/// </summary>
[ExcludeFromCodeCoverage]
public class NotPerformedReasonResult
{
    public NotPerformedReasonResult(NotPerformedReason notPerformedReason, string reason, string reasonNotes, string reasonType)
    {
        NotPerformedReason = notPerformedReason;
        Reason = reason;
        ReasonNotes = reasonNotes;
        ReasonType = reasonType;
    }

    /// <summary>
    /// Gets or sets the NotPerformReason entity of the evaluation
    /// </summary>
    public NotPerformedReason NotPerformedReason { get; set; }

    /// <summary>
    /// Gets or sets the Reason value for an evaluation not happening
    /// </summary>
    public string Reason { get; set; }

    /// <summary>
    /// Gets or sets the Reason Notes value for an evaluation not happening
    /// </summary>
    public string ReasonNotes { get; set; }

    /// <summary>
    /// Gets or sets the Reason Type value for an evaluation not happening
    /// </summary>
    public string ReasonType { get; set; }
}