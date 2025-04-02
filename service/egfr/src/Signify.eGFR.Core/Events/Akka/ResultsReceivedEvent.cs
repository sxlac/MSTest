using System;
using Signify.eGFR.Core.Constants;

namespace Signify.eGFR.Core.Events.Akka;

/// <summary>
/// Event published to Kafka for downstream consumers and reporting
/// </summary>
public class ResultsReceived
{
	// Disable warning "does not access instance data and can be marked as static" - must be instance data for it to be serialized
#pragma warning disable CA1822
	public string ProductCode => ProductCodes.eGFR;
#pragma warning restore CA1822

	/// <summary>
	/// Identifier of the evaluation
	/// </summary>
	public long EvaluationId { get; set; }

	/// <summary>
	/// UTC timestamp this exam was performed
	/// </summary>
	public DateTimeOffset? PerformedDate { get; set; }

	/// <summary>
	/// UTC timestamp results were received for this exam
	/// </summary>
	public DateTimeOffset? ReceivedDate { get; set; }

	/// <summary>
	/// Overall normality/pathology determination of the results
	/// </summary>
	public string Determination { get; set; }

	/// <summary>
	/// Whether or not these results qualify for billing
	/// </summary>
	public bool IsBillable { get; set; }
        
	/// <summary>
	/// Whether or not these results qualify for billing
	/// </summary>
	public Group Result { get; set; }
}

public class Group
{
	/// <summary>
	/// eGFRResult of the Lab Result
	/// </summary>
	public decimal?  Result { get; set; }
	/// <summary>
	/// This is populated for the result description
	/// </summary>
	public string Description { get; set; }
	/// <summary>
	/// Identifier of the Member Normality in Code based on eGFRResult i.e. N/A/U
	/// </summary>
	public string AbnormalIndicator { get; set; }
}