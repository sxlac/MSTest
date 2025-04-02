using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Signify.FOBT.Svc.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public sealed class FOBTStatusCode
{
    public enum StatusCodes
    {
        FobtPerformed = 1,
        InventoryUpdateRequested = 2,
        InventoryUpdateSuccess = 3,
        InventoryUpdateFail = 4,
        BillRequestSent = 5,
        OrderUpdated = 6,
        ValidLabResultsReceived = 7,
        LabOrderCreated = 8,
        FobtNotPerformed = 9,
        InvalidLabResultsReceived = 10,
        ClientPdfDelivered = 11,
        LeftBehindBillRequestSent = 12,
        ResultsBillRequestSent = 13,
        BillRequestNotSent = 14,
        ProviderPayableEventReceived = 15,
        ProviderNonPayableEventReceived = 16,
        ProviderPayRequestSent = 17,
        CdiPassedReceived = 18,
        CdiFailedWithPayReceived = 19,
        CdiFailedWithoutPayReceived = 20,
        OrderHeld = 21,
    }

    public static readonly FOBTStatusCode FOBTPerformed = new FOBTStatusCode((int)StatusCodes.FobtPerformed, "FOBTPerformed");
    public static readonly FOBTStatusCode InventoryUpdateRequested = new FOBTStatusCode((int)StatusCodes.InventoryUpdateRequested, "InventoryUpdateRequested");
    public static readonly FOBTStatusCode InventoryUpdateSuccess = new FOBTStatusCode((int)StatusCodes.InventoryUpdateSuccess, "InventoryUpdateSuccess");
    public static readonly FOBTStatusCode InventoryUpdateFail = new FOBTStatusCode((int)StatusCodes.InventoryUpdateFail, "InventoryUpdateFail");
    public static readonly FOBTStatusCode BillRequestSent = new FOBTStatusCode((int)StatusCodes.BillRequestSent, "BillRequestSent");
    public static readonly FOBTStatusCode OrderUpdated = new FOBTStatusCode((int)StatusCodes.OrderUpdated, "OrderUpdated");
    public static readonly FOBTStatusCode ValidLabResultsReceived = new FOBTStatusCode((int)StatusCodes.ValidLabResultsReceived, "ValidLabResultsReceived");
    public static readonly FOBTStatusCode LabOrderCreated = new FOBTStatusCode((int)StatusCodes.LabOrderCreated, "LabOrderCreated");
    public static readonly FOBTStatusCode FOBTNotPerformed = new FOBTStatusCode((int)StatusCodes.FobtNotPerformed, "FOBTNotPerformed");
    public static readonly FOBTStatusCode InvalidLabResultsReceived = new FOBTStatusCode((int)StatusCodes.InvalidLabResultsReceived, "InvalidLabResultsReceived");
    public static readonly FOBTStatusCode ClientPDFDelivered = new FOBTStatusCode((int)StatusCodes.ClientPdfDelivered, "ClientPDFDelivered");
    public static readonly FOBTStatusCode LeftBehindBillRequestSent = new FOBTStatusCode((int)StatusCodes.LeftBehindBillRequestSent, "FOBT-Left");
    public static readonly FOBTStatusCode ResultsBillRequestSent = new FOBTStatusCode((int)StatusCodes.ResultsBillRequestSent, "FOBT-Results");
    public static readonly FOBTStatusCode BillRequestNotSent = new FOBTStatusCode((int)StatusCodes.BillRequestNotSent, "BillRequestNotSent");
    public static readonly FOBTStatusCode ProviderPayableEventReceived = new FOBTStatusCode((int)StatusCodes.ProviderPayableEventReceived, "ProviderPayableEventReceived");
    public static readonly FOBTStatusCode ProviderNonPayableEventReceived = new FOBTStatusCode((int)StatusCodes.ProviderNonPayableEventReceived, "ProviderNonPayableEventReceived");
    public static readonly FOBTStatusCode ProviderPayRequestSent = new FOBTStatusCode((int)StatusCodes.ProviderPayRequestSent, "ProviderPayRequestSent");
    public static readonly FOBTStatusCode CdiPassedReceived = new FOBTStatusCode((int)StatusCodes.CdiPassedReceived, "CdiPassedReceived");
    public static readonly FOBTStatusCode CdiFailedWithPayReceived = new FOBTStatusCode((int)StatusCodes.CdiFailedWithPayReceived, "CdiFailedWithPayReceived");
    public static readonly FOBTStatusCode CdiFailedWithoutPayReceived = new FOBTStatusCode((int)StatusCodes.CdiFailedWithoutPayReceived, "CdiFailedWithoutPayReceived");
    public static readonly FOBTStatusCode OrderHeld = new FOBTStatusCode((int)StatusCodes.OrderHeld, "OrderHeld");

    public FOBTStatusCode()
    {

    }

    private FOBTStatusCode(int fobtStatusCodeId, string statusCode)
    {
        FOBTStatusCodeId = fobtStatusCodeId;
        StatusCode = statusCode;
    }

    public int FOBTStatusCodeId { get; }
    public string StatusCode { get; }

    public static readonly IReadOnlyCollection<FOBTStatusCode> All =
        new List<FOBTStatusCode>(new[]
            {
                FOBTPerformed,
                InventoryUpdateRequested,
                InventoryUpdateSuccess,
                InventoryUpdateFail,
                ValidLabResultsReceived,
                OrderUpdated,
                BillRequestSent,
                LabOrderCreated,
                FOBTNotPerformed,
                InvalidLabResultsReceived,
                ClientPDFDelivered,
                LeftBehindBillRequestSent,
                ResultsBillRequestSent,
                BillRequestNotSent,
                ProviderPayableEventReceived,
                ProviderNonPayableEventReceived,
                ProviderPayRequestSent,
                CdiPassedReceived,
                CdiFailedWithPayReceived,
                CdiFailedWithoutPayReceived,
                OrderHeld
            }
        );

    public static FOBTStatusCode GetFOBTStatusCode(string code)
    {
        return All.FirstOrDefault(x => x.StatusCode.Equals(code, StringComparison.OrdinalIgnoreCase));
    }

    public static FOBTStatusCode GetFOBTStatusCode(int id)
    {
        return All.FirstOrDefault(x => x.FOBTStatusCodeId == id);
    }

    public override string ToString()
    {
        return $"{nameof(FOBTStatusCodeId)}: {FOBTStatusCodeId}, {nameof(StatusCode)}: {StatusCode}";
    }

    private bool Equals(FOBTStatusCode other)
    {
        return FOBTStatusCodeId == other.FOBTStatusCodeId && StatusCode == other.StatusCode;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((FOBTStatusCode)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (FOBTStatusCodeId * 397) ^ (StatusCode != null ? StatusCode.GetHashCode() : 0);
        }
    }
}