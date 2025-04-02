using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.FOBT.Svc.Core.Data.Entities;

/// <summary>
/// Details explaining why a FOBT exam that a provider was scheduled to perform was not
/// actually performed
/// </summary>
[ExcludeFromCodeCoverage]
public class FOBTNotPerformed : IEquatable<FOBTNotPerformed>
{
    /// <summary>
    /// Identifier of this record
    /// </summary>
    public int FOBTNotPerformedId { get; set; }
    /// <summary>
    /// Foreign key identifier of the corresponding <see cref="FOBT"/>
    /// </summary>
    public int FOBTId { get; set; }
    /// <summary>
    /// Foreign key identifier of the corresponding <see cref="NotPerformedReason"/>
    /// </summary>
    public short NotPerformedReasonId { get; set; }
    /// <summary>
    /// Date and time this record was created
    /// </summary>
    public DateTime CreatedDateTime { get; set; }

    /// <summary>
    /// Gets or sets the Reason Notes for not performed
    /// </summary>
    public string Notes { get; set; }

    #region IEquality
    public bool Equals(FOBTNotPerformed other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return FOBTNotPerformedId == other.FOBTNotPerformedId
               && FOBTId == other.FOBTId
               && NotPerformedReasonId == other.NotPerformedReasonId
               && CreatedDateTime.Equals(other.CreatedDateTime);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((FOBTNotPerformed)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(FOBTNotPerformedId, FOBTId, NotPerformedReasonId, CreatedDateTime);
    }
    #endregion
    public virtual NotPerformedReason NotPerformedReason { get; set; }
    public virtual FOBT FOBT { get; set; }
}