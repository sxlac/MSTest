using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.FOBT.Svc.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public class FOBTStatus
{
	public FOBTStatus()
	{
            
	}

	public int FOBTStatusId { get; set; }

	//Foreign key 
	public virtual FOBTStatusCode FOBTStatusCode { get; set; }

	//Foreign key
	public virtual FOBT FOBT { get; set; }

	public int FOBTId { get; set; }
	public int FOBTStatusCodeId { get; set; }

	public DateTimeOffset CreatedDateTime { get; set; }


	public FOBTStatus(int fobtStatusId, FOBTStatusCode fobtStatusCode, FOBT fobt, DateTimeOffset createdDateTime)
	{
		FOBTStatusId = fobtStatusId;
		FOBTStatusCode = fobtStatusCode;
		FOBT = fobt;
		CreatedDateTime = createdDateTime;
	}

	protected bool Equals(FOBTStatus other)
	{
		return FOBTStatusId == other.FOBTStatusId && Equals(FOBTStatusCode, other.FOBTStatusCode) && Equals(FOBT, other.FOBT) && CreatedDateTime.Equals(other.CreatedDateTime);
	}

	public override bool Equals(object obj)
	{
		if (ReferenceEquals(null, obj)) return false;
		if (ReferenceEquals(this, obj)) return true;
		if (obj.GetType() != this.GetType()) return false;
		return Equals((FOBTStatus) obj);
	}

	public override int GetHashCode()
	{
		unchecked
		{
			var hashCode = FOBTStatusId;
			hashCode = (hashCode * 397) ^ (FOBTStatusCode != null ? FOBTStatusCode.GetHashCode() : 0);
			hashCode = (hashCode * 397) ^ (FOBT != null ? FOBT.GetHashCode() : 0);
			hashCode = (hashCode * 397) ^ CreatedDateTime.GetHashCode();
			return hashCode;
		}
	}

	public override string ToString()
	{
		return $"{nameof(FOBTStatusId)}: {FOBTStatusId}, {nameof(FOBTStatusCode)}: {FOBTStatusCode}, {nameof(FOBT)}: {FOBT}, {nameof(CreatedDateTime)}: {CreatedDateTime}";
	}
}