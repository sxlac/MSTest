using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Signify.DEE.Svc.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public class DeeNotPerformed : IEqualityComparer<DeeNotPerformed>
{
    /// <summary>
    /// Identifier of this record
    /// </summary>
    public int DeeNotPerformedId { get; set; }
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
    /// <summary>
    /// Free Form Notes. More of like a description
    /// </summary>
    public string Notes { get; set; }
    public virtual NotPerformedReason NotPerformedReason { get; set; }
    public virtual Exam Exam { get; set; }

    #region IEquality
    public bool Equals(DeeNotPerformed x, DeeNotPerformed y)
    {
        if (ReferenceEquals(x, y)) return true;
        return x.DeeNotPerformedId == y.DeeNotPerformedId
               && x.ExamId == y.ExamId
               && x.NotPerformedReasonId == y.NotPerformedReasonId
               && x.CreatedDateTime.Equals(y.CreatedDateTime)
               && x.Notes.Equals(y.Notes);
    }

    public int GetHashCode([DisallowNull] DeeNotPerformed obj)
        => HashCode.Combine(DeeNotPerformedId, ExamId, NotPerformedReasonId, CreatedDateTime,Notes);
    #endregion

}