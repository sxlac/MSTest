using Signify.CKD.Svc.Core.Data.Entities;
using System;

namespace Signify.CKD.Svc.Core.Models;

public class EvaluationAnswers
{
    public bool IsCKDEvaluation { get; set; }
    public LookupCKDAnswer LookupCKDAnswerEntity { get; set; }
    public DateTime? ExpirationDate { get; set; }
    /// <summary>
    /// PK identifier of the reason in the NotPerformedReason db table
    /// </summary>
    /// <remarks>
    /// Null if the lab was performed, or if not performed and the reason was not given or does not exist in db
    /// </remarks>
    public short? NotPerformedReasonId { get; set; }
    /// <summary>
    /// If the lab was not performed, the AnswerId from the evaluation as to why the lab was not performed
    /// </summary>
    public int? NotPerformedAnswerId { get; set; }
    /// <summary>
    /// Gets or sets the Reason Notes for not performed
    /// </summary>
    public string NotPerformedNotes { get; set; }
}
