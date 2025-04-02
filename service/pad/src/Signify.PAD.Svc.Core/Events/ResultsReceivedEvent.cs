using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Signify.PAD.Svc.Core.Constants;

namespace Signify.PAD.Svc.Core.Events;

[ExcludeFromCodeCoverage]
public sealed class ResultsReceived : IEquatable<ResultsReceived>
{
	public string ProductCode { get; } = Application.ProductCode;
	public int EvaluationId { get; set; }
	/// <summary>
	/// UTC timestamp this lab was performed
	/// </summary>
	public DateTimeOffset PerformedDate { get; set; }
	/// <summary>
	/// UTC timestamp results were received for this exam
	/// </summary>
	public DateTimeOffset ReceivedDate { get; set; }
	/// <summary>
	/// Whether or not this exam qualifies being billed
	/// </summary>
	public bool IsBillable { get; set; }
	/// <summary>
	/// Overall normality/pathology determination of the results
	/// </summary>
	/// <remarks>Value range is of NormalityIndicator</remarks>
	public string Determination { get; set; }
	public List<SideResultInfo> Results { get; set; } = new List<SideResultInfo>();

	#region IEquatable
	private static bool ResultsEqual(IReadOnlyCollection<SideResultInfo> lhs, IReadOnlyList<SideResultInfo> rhs)
	{
		if (ReferenceEquals(lhs, rhs)) return true;
		if (ReferenceEquals(lhs, null)) return false;
		if (ReferenceEquals(null, rhs)) return false;
		if (lhs.Count != rhs.Count) return false;
		return !lhs.Where((t, i) => t != rhs[i]).Any();
	}

	public bool Equals(ResultsReceived other)
	{
		if (ReferenceEquals(null, other)) return false;
		if (ReferenceEquals(this, other)) return true;
		return EvaluationId == other.EvaluationId && PerformedDate.Equals(other.PerformedDate) && ReceivedDate.Equals(other.ReceivedDate) && IsBillable == other.IsBillable && Determination == other.Determination && ResultsEqual(Results, other.Results);
	}

	public override bool Equals(object obj)
	{
		if (ReferenceEquals(null, obj)) return false;
		if (ReferenceEquals(this, obj)) return true;
		if (obj.GetType() != GetType()) return false;
		return Equals((ResultsReceived) obj);
	}

	public override int GetHashCode()
	{
		unchecked
		{
			var hashCode = EvaluationId;
			hashCode = (hashCode * 397) ^ PerformedDate.GetHashCode();
			hashCode = (hashCode * 397) ^ ReceivedDate.GetHashCode();
			hashCode = (hashCode * 397) ^ IsBillable.GetHashCode();
			hashCode = (hashCode * 397) ^ (Determination != null ? Determination.GetHashCode() : 0);
			hashCode = (hashCode * 397) ^ (Results != null ? Results.GetHashCode() : 0);
			return hashCode;
		}
	}

	public static bool operator ==(ResultsReceived left, ResultsReceived right)
	{
		return Equals(left, right);
	}

	public static bool operator !=(ResultsReceived left, ResultsReceived right)
	{
		return !Equals(left, right);
	}
	#endregion IEquatable
}

public sealed class SideResultInfo : IEquatable<SideResultInfo>
{
	public string Side { get; set; }
	public string Result { get; set; }
	public string Severity { get; set; }
	/// <remarks>
	/// Same thing as NormalityIndicator; downstream consumers just want the message property to be called this instead
	/// </remarks>
	public string AbnormalIndicator { get; set; }
	public string Exception { get; set; }

	#region IEquatable
	public bool Equals(SideResultInfo other)
	{
		if (ReferenceEquals(null, other)) return false;
		if (ReferenceEquals(this, other)) return true;
		return Side == other.Side && Result == other.Result && Severity == other.Severity && AbnormalIndicator == other.AbnormalIndicator && Exception == other.Exception;
	}

	public override bool Equals(object obj)
	{
		if (ReferenceEquals(null, obj)) return false;
		if (ReferenceEquals(this, obj)) return true;
		if (obj.GetType() != GetType()) return false;
		return Equals((SideResultInfo) obj);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Side, Result, Severity, AbnormalIndicator, Exception);
	}

	public static bool operator ==(SideResultInfo left, SideResultInfo right)
	{
		return Equals(left, right);
	}

	public static bool operator !=(SideResultInfo left, SideResultInfo right)
	{
		return !Equals(left, right);
	}
	#endregion IEquatable
}