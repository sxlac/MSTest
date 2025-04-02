using System;
using System.ComponentModel.DataAnnotations;
using Signify.eGFR.Core.Constants;

namespace Signify.eGFR.Core.Data.Entities;

/// <summary>
/// Details explaining why a eGFR exam that a provider was scheduled to perform was not
/// actually performed
/// </summary>
public class ExamNotPerformed
{
    /// <summary>
    /// Identifier of this record
    /// </summary>
    public int ExamNotPerformedId { get; set; }
    /// <summary>
    /// Foreign key identifier of the corresponding <see cref="Exam"/>
    /// </summary>
    public int ExamId { get; set; }
    /// <summary>
    /// Foreign key identifier of the corresponding <see cref="NotPerformedReason"/>
    /// </summary>
    public short NotPerformedReasonId { get; set; }
    /// <summary>
    /// Date and time this record was created
    /// </summary>
    public DateTimeOffset CreatedDateTime { get; set; }

    public virtual NotPerformedReason NotPerformedReason { get; set; }
    public virtual Exam Exam { get; set; }
    
    /// <summary>
    /// Gets or sets the Reason Notes for not performed
    /// </summary>
    [StringLength(Application.MaxNotPerformedReasonNotesLength)]
    public string Notes { get; set; }
}