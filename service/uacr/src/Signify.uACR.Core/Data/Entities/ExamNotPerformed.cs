using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Signify.uACR.Core.Constants;

namespace Signify.uACR.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public class ExamNotPerformed
{
    public int ExamNotPerformedId { get; set; }
    public int ExamId { get; set; }
    public short NotPerformedReasonId { get; set; }
    public DateTimeOffset CreatedDateTime { get; set; }

    public virtual Exam Exam { get; set; }
    public virtual NotPerformedReason NotPerformedReason { get; set; }
    
    /// <summary>
    /// Gets or sets the Reason Notes for not performed
    /// </summary>
    [StringLength(Application.MaxNotPerformedReasonNotesLength)]
    public string Notes { get; set; }
}