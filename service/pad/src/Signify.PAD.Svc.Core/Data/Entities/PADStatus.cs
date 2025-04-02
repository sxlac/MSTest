using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public class PADStatus
{
    public PADStatus()
    {
    }

    public PADStatus(int padStatusId, int padStatusCodeId, int padId, DateTimeOffset createdDateTime)
    {
        PADStatusId = padStatusId;
        PADStatusCodeId = padStatusCodeId;
        PADId = padId;
        CreatedDateTime = createdDateTime;
    }

    public int PADStatusId { get; set; }
       
    public virtual PADStatusCode PADStatusCode { get; set; }
    
    public int PADStatusCodeId { get; set; }

    //Foreign key
    public virtual PAD PAD { get; set; }
        
    public int PADId { get; set; }

    public DateTimeOffset CreatedDateTime { get; set; }

    protected bool Equals(PADStatus other)
        => PADStatusId == other.PADStatusId && PADStatusCodeId == other.PADStatusCodeId && PADId == other.PADId &&
           CreatedDateTime.Equals(other.CreatedDateTime);

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((PADStatus)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = PADStatusId;
            hashCode = (hashCode * 397) ^ PADStatusCodeId.GetHashCode();
            hashCode = (hashCode * 397) ^ PADId.GetHashCode();
            hashCode = (hashCode * 397) ^ CreatedDateTime.GetHashCode();
            return hashCode;
        }
    }

    public override string ToString()
        => $"{nameof(PADStatusId)}: {PADStatusId}, {nameof(PADStatusCodeId)}: {PADStatusCodeId}, {nameof(PADId)}: {PADId}, {nameof(CreatedDateTime)}: {CreatedDateTime}";
}