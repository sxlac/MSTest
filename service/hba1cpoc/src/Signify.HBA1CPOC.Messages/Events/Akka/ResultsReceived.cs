using System;

namespace Signify.HBA1CPOC.Messages.Events.Akka
{
	public class ResultsReceived
	{
		/// <summary>
		/// Signify Product Code for this event
		/// </summary>
		public string ProductCode { get; set; }

		/// <summary>
		/// Unique identifier of the evaluation this event corresponds to
		/// </summary>
		public long EvaluationId { get; set; }

		/// <summary>
		/// UTC timestamp this evaluation was finalized on the provider's iPad (not necessarily when the Signify
		/// Evaluation API received the message, for ex in the case of the iPad being offline)
		/// </summary>
		public DateTimeOffset PerformedDate { get; set; }

		/// <summary>
		/// UTC timestamp results for this product and evaluation were received by the Signify Evaluation API
		/// </summary>
		public DateTimeOffset ReceivedDate { get; set; }

		/// <summary>
		/// Whether or not this is a billable event
		/// </summary>
		public bool IsBillable { get; set; }

		/// <summary>
		/// ie Normality/Abnormality Indicator
		/// </summary>
		/// <remarks>
		///	Possible values are "N" (normal), "A" (abnormal), "U" (undetermined)
		///
		/// Value should be the same as <see cref="Results"/>.AbnormalIndicator
		/// </remarks>
		public string Determination { get; set; }

		/// <summary>
		/// Details of these results
		/// </summary>
		public ResultInfo Results { get; set; }
	}

	public class ResultInfo
	{
		public string Result { get; set; }

		/// <summary>
		/// ie Determination or Normality Indicator
		/// </summary>
		public string AbnormalIndicator { get; set; }

		public string Exception { get; set; }
	}
}
