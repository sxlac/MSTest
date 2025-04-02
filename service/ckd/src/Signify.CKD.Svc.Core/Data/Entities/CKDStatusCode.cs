using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Signify.CKD.Svc.Core.Models;

namespace Signify.CKD.Svc.Core.Data.Entities;

public sealed class CKDStatusCode
{
	public static readonly CKDStatusCode CKDPerformed = new ((int)StatusCodes.CKDPerformed, "CKDPerformed");
	public static readonly CKDStatusCode InventoryUpdateRequested = new ((int)StatusCodes.InventoryUpdateRequested, "InventoryUpdateRequested");
	public static readonly CKDStatusCode InventoryUpdateSuccess = new ((int)StatusCodes.InventoryUpdateSuccess, "InventoryUpdateSuccess");
	public static readonly CKDStatusCode InventoryUpdateFail = new ((int)StatusCodes.InventoryUpdateFail, "InventoryUpdateFail");
	public static readonly CKDStatusCode BillRequestSent = new ((int)StatusCodes.BillRequestSent, "BillRequestSent");
	public static readonly CKDStatusCode BillableEventRecieved = new ((int)StatusCodes.BillableEventRecieved, "BillableEventRecieved");
	public static readonly CKDStatusCode CKDNotPerformed = new ((int)StatusCodes.CKDNotPerformed, "CKDNotPerformed");
	public static readonly CKDStatusCode BillRequestNotSent = new ((int)StatusCodes.BillRequestNotSent, "BillRequestNotSent");
	public static readonly CKDStatusCode ProviderPayableEventReceived = new ((int)StatusCodes.ProviderPayableEventReceived, "ProviderPayableEventReceived");
	public static readonly CKDStatusCode ProviderPayRequestSent = new ((int)StatusCodes.ProviderPayRequestSent, "ProviderPayRequestSent");
	public static readonly CKDStatusCode CdiPassedReceived = new((int)StatusCodes.CdiPassedReceived, "CdiPassedReceived");
	public static readonly CKDStatusCode CdiFailedWithPayReceived = new((int)StatusCodes.CdiFailedWithPayReceived, "CdiFailedWithPayReceived");
	public static readonly CKDStatusCode CdiFailedWithoutPayReceived = new((int)StatusCodes.CdiFailedWithoutPayReceived, "CdiFailedWithoutPayReceived");
	public static readonly CKDStatusCode ProviderNonPayableEventReceived = new((int) StatusCodes.ProviderNonPayableEventReceived, "ProviderNonPayableEventReceived");

	internal CKDStatusCode() // Needed for Fixture in unit tests
	{
	}

	public CKDStatusCode(int ckdStatusCodeId, string statusCode)
	{
		CKDStatusCodeId = ckdStatusCodeId;
		StatusCode = statusCode;
	}

	[Key] public int CKDStatusCodeId { get; }
	public string StatusCode { get; }

	public static readonly IReadOnlyList<CKDStatusCode> All = new[]
			{
				CKDPerformed,
				InventoryUpdateRequested,
				InventoryUpdateSuccess,
				InventoryUpdateFail,
				BillRequestSent,
				BillableEventRecieved,
				CKDNotPerformed,
				BillRequestNotSent,
				ProviderPayableEventReceived,
				ProviderPayRequestSent,
				CdiPassedReceived,
				CdiFailedWithPayReceived,
				CdiFailedWithoutPayReceived,
				ProviderNonPayableEventReceived
			};

	public static CKDStatusCode GetCKDStatusCode(string code)
	{
		return All.FirstOrDefault(x => x.StatusCode.Equals(code, StringComparison.OrdinalIgnoreCase));
	}

	public static CKDStatusCode GetCKDStatusCode(int id)
	{
		return All.FirstOrDefault(x => x.CKDStatusCodeId == id);
	}

	public override string ToString()
	{
		return $"{nameof(CKDStatusCodeId)}: {CKDStatusCodeId}, {nameof(StatusCode)}: {StatusCode}";
	}

	private bool Equals(CKDStatusCode other)
	{
		return CKDStatusCodeId == other.CKDStatusCodeId && StatusCode == other.StatusCode;
	}

	public override bool Equals(object obj)
	{
		if (ReferenceEquals(null, obj)) return false;
		if (ReferenceEquals(this, obj)) return true;
		if (obj.GetType() != this.GetType()) return false;
		return Equals((CKDStatusCode) obj);
	}

	public override int GetHashCode()
	{
		unchecked
		{
			return (CKDStatusCodeId * 397) ^ (StatusCode != null ? StatusCode.GetHashCode() : 0);
		}
	}
}