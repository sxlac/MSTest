using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Signify.HBA1CPOC.Svc.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public sealed class HBA1CPOCStatusCode
{
    private enum StatusCodes
    {
        HBA1CPOCPerformed = 1,
        InventoryUpdateRequested = 2,
        InventoryUpdateSuccess = 3,
        InventoryUpdateFail = 4,
        BillRequestSent = 5,
        BillableEventRecieved = 6,
        HBA1CPOCNotPerformed = 7,
        ProviderPayableEventReceived = 8,
        ProviderPayRequestSent = 9,
        CdiPassedReceived = 10,
        CdiFailedWithPayReceived = 11,
        CdiFailedWithoutPayReceived = 12,
        ProviderNonPayableEventReceived = 13,
        BillRequestNotSent = 14
    }

    public static readonly HBA1CPOCStatusCode HBA1CPOCPerformed = new((int)StatusCodes.HBA1CPOCPerformed, "HBA1CPOCPerformed");
    public static readonly HBA1CPOCStatusCode HBA1CPOCNotPerformed = new((int)StatusCodes.HBA1CPOCNotPerformed, "HBA1CPOCNotPerformed");
    public static readonly HBA1CPOCStatusCode InventoryUpdateRequested = new((int)StatusCodes.InventoryUpdateRequested, "InventoryUpdateRequested");
    public static readonly HBA1CPOCStatusCode InventoryUpdateSuccess = new((int)StatusCodes.InventoryUpdateSuccess, "InventoryUpdateSuccess");
    public static readonly HBA1CPOCStatusCode InventoryUpdateFail = new((int)StatusCodes.InventoryUpdateFail, "InventoryUpdateFail");
    public static readonly HBA1CPOCStatusCode BillRequestSent = new((int)StatusCodes.BillRequestSent, "BillRequestSent");
    public static readonly HBA1CPOCStatusCode BillableEventRecieved = new((int)StatusCodes.BillableEventRecieved, "BillableEventRecieved");
    public static readonly HBA1CPOCStatusCode ProviderPayableEventReceived = new((int)StatusCodes.ProviderPayableEventReceived, "ProviderPayableEventReceived");
    public static readonly HBA1CPOCStatusCode ProviderPayRequestSent = new((int)StatusCodes.ProviderPayRequestSent, "ProviderPayRequestSent");
    public static readonly HBA1CPOCStatusCode CdiPassedReceived = new((int)StatusCodes.CdiPassedReceived, "CdiPassedReceived");
    public static readonly HBA1CPOCStatusCode CdiFailedWithPayReceived = new((int)StatusCodes.CdiFailedWithPayReceived, "CdiFailedWithPayReceived");
    public static readonly HBA1CPOCStatusCode CdiFailedWithoutPayReceived = new((int)StatusCodes.CdiFailedWithoutPayReceived, "CdiFailedWithoutPayReceived");
    public static readonly HBA1CPOCStatusCode ProviderNonPayableEventReceived = new((int)StatusCodes.ProviderNonPayableEventReceived, "ProviderNonPayableEventReceived");
    public static readonly HBA1CPOCStatusCode BillRequestNotSent = new((int)StatusCodes.BillRequestNotSent, "BillRequestNotSent");


    internal HBA1CPOCStatusCode()
    {
    }

    public HBA1CPOCStatusCode(int hba1cpocStatusCodeId, string statusCodeName)
    {
        HBA1CPOCStatusCodeId = hba1cpocStatusCodeId;
        StatusCodeName = statusCodeName;
    }

    public int HBA1CPOCStatusCodeId { get; }
    public string StatusCodeName { get; }

    private static readonly IReadOnlyList<HBA1CPOCStatusCode> All =
        new List<HBA1CPOCStatusCode>(new[]
            {
                HBA1CPOCPerformed,
                InventoryUpdateRequested,
                InventoryUpdateSuccess,
                InventoryUpdateFail,
                BillRequestSent,
                BillableEventRecieved,
                HBA1CPOCNotPerformed,
                ProviderPayableEventReceived,
                ProviderPayRequestSent,
                CdiPassedReceived,
                CdiFailedWithPayReceived,
                CdiFailedWithoutPayReceived,
                ProviderNonPayableEventReceived,
                BillRequestNotSent
            }
        );

    public static HBA1CPOCStatusCode GetHBA1CPOCStatusCode(string code)
    {
        return All.FirstOrDefault(x => x.StatusCodeName.Equals(code, StringComparison.OrdinalIgnoreCase));
    }

    public static HBA1CPOCStatusCode GetHBA1CPOCStatusCode(int id)
    {
        return All.FirstOrDefault(x => x.HBA1CPOCStatusCodeId == id);
    }

    public override string ToString()
    {
        return $"{nameof(HBA1CPOCStatusCodeId)}: {HBA1CPOCStatusCodeId}, {nameof(StatusCodeName)}: {StatusCodeName}";
    }

    private bool Equals(HBA1CPOCStatusCode other)
    {
        return HBA1CPOCStatusCodeId == other.HBA1CPOCStatusCodeId && StatusCodeName == other.StatusCodeName;
    }

    public override bool Equals(object obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == this.GetType() && Equals((HBA1CPOCStatusCode)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (HBA1CPOCStatusCodeId * 397) ^ (StatusCodeName != null ? StatusCodeName.GetHashCode() : 0);
        }
    }
}