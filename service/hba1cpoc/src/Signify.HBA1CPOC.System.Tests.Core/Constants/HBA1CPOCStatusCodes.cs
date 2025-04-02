using Signify.HBA1CPOC.System.Tests.Core.Models.Database;

namespace Signify.HBA1CPOC.System.Tests.Core.Constants;

public static class HBA1CPOCStatusCodes
{
    public static readonly HBA1CPOCStatusCode HBA1CPOCPerformed = new(1, "HBA1CPOCPerformed");
    public static readonly HBA1CPOCStatusCode InventoryUpdateRequested = new(2, "InventoryUpdateRequested");
    public static readonly HBA1CPOCStatusCode InventoryUpdateSuccess = new(3, "InventoryUpdateSuccess");
    public static readonly HBA1CPOCStatusCode InventoryUpdateFail = new(4, "InventoryUpdateFail");
    public static readonly HBA1CPOCStatusCode BillRequestSent = new(5, "BillRequestSent");
    public static readonly HBA1CPOCStatusCode BillableEventRecieved = new(6, "BillableEventRecieved");
    public static readonly HBA1CPOCStatusCode HBA1CPOCNotPerformed = new(7, "HBA1CPOCNotPerformed");
    public static readonly HBA1CPOCStatusCode ProviderPayableEventReceived = new(8, "ProviderPayableEventReceived");
    public static readonly HBA1CPOCStatusCode ProviderPayRequestSent = new(9, "ProviderPayRequestSent");
    public static readonly HBA1CPOCStatusCode CdiPassedReceived = new(10, "CdiPassedReceived");
    public static readonly HBA1CPOCStatusCode CdiFailedWithPayReceived = new(11, "CdiFailedWithPayReceived");
    public static readonly HBA1CPOCStatusCode CdiFailedWithoutPayReceived = new(12, "CdiFailedWithoutPayReceived");
    public static readonly HBA1CPOCStatusCode ProviderNonPayableEventReceived = new(13, "ProviderNonPayableEventReceived");
    public static readonly HBA1CPOCStatusCode BillRequestNotSent = new(14, "BillRequestNotSent");
    
}