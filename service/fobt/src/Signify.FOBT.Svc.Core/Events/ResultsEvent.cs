using Signify.FOBT.Svc.Core.Constants;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Signify.FOBT.Svc.Core.Events;
// Tech debt - this class/event name doesn't match our other PM's...
// It's a breaking change, though, so need to coordinate with consumers.

/// <summary>
/// Event published to Kafka for downstream consumers and reporting
/// </summary>
[ExcludeFromCodeCoverage]
public class Results
{
	// Disable warning "does not access instance data and can be marked as static" - must be instance data for it to be serialized
#pragma warning disable CA1822
	public string ProductCode => ApplicationConstants.PRODUCT_CODE;
#pragma warning restore CA1822

	public int EvaluationId { get; set; }

	/// <summary>
	/// UTC timestamp this exam was performed
	/// </summary>
	public DateTime? PerformedDate { get; set; }

	/// <summary>
	/// UTC timestamp results were received for this exam
	/// </summary>
	public DateTime? ReceivedDate { get; set; }

	/// <summary>
	/// UTC timestamp of the member collection date
	/// </summary>
	public DateTime? MemberCollectionDate { get; set; }

	/// <summary>
	/// Overall normality/pathology determination of the results
	/// </summary>
	public string Determination { get; set; }

	public string Barcode { get; set; }

	/// <summary>
	/// Whether or not these results qualify for billing
	/// </summary>
	public bool IsBillable { get; set; }

	// Tech debt - this property name doesn't match our other PM's...
	// It's a breaking change, though, so need to coordinate downstream.
	public List<Group> Result { get; set; } = new List<Group>();
}

[ExcludeFromCodeCoverage]
public class Group
{
	public string Result { get; set; }
	public string Exception { get; set; }
	public string AbnormalIndicator { get; set; }
}