using Signify.FOBT.Svc.System.Tests.Core.Models.Database;

namespace Signify.FOBT.Svc.System.Tests.Core.Constants;


public static class ExamStatusCodes
{
    public static readonly FOBTStatusCode ExamPerformed = new(1, "FOBTPerformed");
    public static readonly FOBTStatusCode InventoryUpdateRequested = new(2, "InventoryUpdateRequested");
    public static readonly FOBTStatusCode InventoryUpdateSuccess = new(3, "InventoryUpdateSuccess");
    public static readonly FOBTStatusCode InventoryUpdateFail = new(4, "InventoryUpdateFail");
    public static readonly FOBTStatusCode BillRequestSent = new(5, "BillRequestSent");
    public static readonly FOBTStatusCode OrderUpdated = new(6, "OrderUpdated");
    public static readonly FOBTStatusCode ValidLabResultsReceived = new(7, "ValidLabResultsReceived");
    public static readonly FOBTStatusCode LabOrderCreated = new(8, "LabOrderCreated");
    public static readonly FOBTStatusCode FOBTNotPerformed = new(9, "FOBTNotPerformed");
    public static readonly FOBTStatusCode InvalidLabResultsReceived = new(10, "InvalidLabResultsReceived");
    public static readonly FOBTStatusCode ClientPDFDelivered  = new(11, "ClientPDFDelivered");
    public static readonly FOBTStatusCode FOBTLeft = new(12, "FOBT-Left");
    public static readonly FOBTStatusCode FOBTResults = new(13, "FOBT-Results");
    public static readonly FOBTStatusCode BillRequestNotSent = new(14, "BillRequestNotSent");
    public static readonly FOBTStatusCode ProviderPayableEventReceived = new(15, "ProviderPayableEventReceived");
    public static readonly FOBTStatusCode ProviderNonPayableEventReceived = new(16, "ProviderNonPayableEventReceived");
    public static readonly FOBTStatusCode ProviderPayRequestSent = new(17, "ProviderPayRequestSent");
    public static readonly FOBTStatusCode CdiPassedReceived = new(18, "CdiPassedReceived");
    public static readonly FOBTStatusCode CdiFailedWithPayReceived = new(19, "CdiFailedWithPayReceived");
    public static readonly FOBTStatusCode CdiFailedWithoutPayReceived = new(20, "CdiFailedWithoutPayReceived");
    public static readonly FOBTStatusCode OrderHeld = new(21, "OrderHeld");
}