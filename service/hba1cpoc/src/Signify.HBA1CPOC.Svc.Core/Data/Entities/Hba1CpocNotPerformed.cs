using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Signify.HBA1CPOC.Svc.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public class Hba1CpocNotPerformed : IEqualityComparer<Hba1CpocNotPerformed>
{
    /// <summary>
    /// Identifier of this record
    /// </summary>
    public int HBA1CPOCNotPerformedId { get; set; }
    /// <summary>
    /// Foreign key identifier of the corresponding <see cref="HBA1CPOC"/>
    /// </summary>
    public int HBA1CPOCId { get; set; }
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

    public virtual NotPerformedReason NotPerformedReason { get; set; }
    public virtual HBA1CPOC HBA1CPOC { get; set; }

    #region IEquality

    public bool Equals(Hba1CpocNotPerformed x, Hba1CpocNotPerformed y)
    {
        if (ReferenceEquals(x, y)) return true;
        return x.HBA1CPOCNotPerformedId == y.HBA1CPOCNotPerformedId
               && x.HBA1CPOCId == y.HBA1CPOCId
               && x.NotPerformedReasonId == y.NotPerformedReasonId
               && x.CreatedDateTime.Equals(y.CreatedDateTime);
    }

    public int GetHashCode([DisallowNull] Hba1CpocNotPerformed obj)
    {
        return HashCode.Combine(HBA1CPOCNotPerformedId, HBA1CPOCId, NotPerformedReasonId, CreatedDateTime);
    }
    #endregion
}