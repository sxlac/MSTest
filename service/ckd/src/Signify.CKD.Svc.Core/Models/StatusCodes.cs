namespace Signify.CKD.Svc.Core.Models;

public enum StatusCodes
{
    CKDPerformed = 1,
    InventoryUpdateRequested = 2,
    InventoryUpdateSuccess = 3,
    InventoryUpdateFail = 4,
    BillRequestSent = 5,
    BillableEventRecieved = 6,
    CKDNotPerformed = 7,
    BillRequestNotSent = 8,
    ProviderPayableEventReceived = 9,
    ProviderPayRequestSent = 10,
    CdiPassedReceived = 11,
    CdiFailedWithPayReceived = 12,
    CdiFailedWithoutPayReceived = 13,
    ProviderNonPayableEventReceived = 14
}