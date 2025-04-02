using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Signify.PAD.Svc.Core.Models;

namespace Signify.PAD.Svc.Core.Data.Entities;

public class PADStatusCode : IEntityEnum<PADStatusCode>
{
	public static readonly PADStatusCode PadPerformed = new((int)StatusCodes.PadPerformed, "PADPerformed");
	public static readonly PADStatusCode BillRequestSent = new((int)StatusCodes.BillRequestSent, "BillRequestSent");
	public static readonly PADStatusCode BillableEventReceived = new((int)StatusCodes.BillableEventReceived, "BillableEventReceived");
	public static readonly PADStatusCode PadNotPerformed = new((int)StatusCodes.PadNotPerformed, "PADNotPerformed");
	public static readonly PADStatusCode WaveformDocumentDownloaded = new((int)StatusCodes.WaveformDocumentDownloaded, "WaveformDocumentDownloaded");
	public static readonly PADStatusCode WaveformDocumentUploaded = new((int)StatusCodes.WaveformDocumentUploaded, "WaveformDocumentUploaded");
	public static readonly PADStatusCode ProviderPayableEventReceived = new((int)StatusCodes.ProviderPayableEventReceived, "ProviderPayableEventReceived");
	public static readonly PADStatusCode ProviderPayRequestSent = new((int)StatusCodes.ProviderPayRequestSent, "ProviderPayRequestSent");
	public static readonly PADStatusCode CdiPassedReceived = new((int)StatusCodes.CdiPassedReceived, "CdiPassedReceived");
	public static readonly PADStatusCode CdiFailedWithPayReceived = new((int)StatusCodes.CdiFailedWithPayReceived, "CdiFailedWithPayReceived");
	public static readonly PADStatusCode CdiFailedWithoutPayReceived = new((int)StatusCodes.CdiFailedWithoutPayReceived, "CdiFailedWithoutPayReceived");
	public static readonly PADStatusCode ProviderNonPayableEventReceived = new((int)StatusCodes.ProviderNonPayableEventReceived, "ProviderNonPayableEventReceived");
	public static readonly PADStatusCode BillRequestNotSent = new((int)StatusCodes.BillRequestNotSent, nameof(StatusCodes.BillRequestNotSent));

	protected internal PADStatusCode()
	{
	}

	private PADStatusCode(int padStatusCodeId, string statusCode)
	{
		PADStatusCodeId = padStatusCodeId;
		StatusCode = statusCode;
	}

	/// <summary>
	/// Identifier of this status code
	/// </summary>
	[Key]
	public int PADStatusCodeId { get; init; }
	/// <summary>
	/// Short name for the status code
	/// </summary>
	public string StatusCode { get; init; }

	/// <inheritdoc />
	public IEnumerable<PADStatusCode> GetAllEnumerations()
		=> new[]
			{
				PadPerformed,
				BillRequestSent,
				BillableEventReceived,
				PadNotPerformed,
				WaveformDocumentDownloaded,
				WaveformDocumentUploaded,
				ProviderPayableEventReceived,
				ProviderPayRequestSent,
				CdiPassedReceived,
				CdiFailedWithPayReceived,
				CdiFailedWithoutPayReceived,
				ProviderNonPayableEventReceived,
				BillRequestNotSent
			};
}