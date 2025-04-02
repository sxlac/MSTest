namespace Signify.uACR.Core.Models;

public enum StatusCode
{
    ExamPerformed = 1,
    ExamNotPerformed = 2,
    BillableEventReceived = 3,
    BillRequestSent = 4,
    ClientPdfDelivered = 5,
    LabResultsReceived = 6,
    BillRequestNotSent = 7,
    ProviderPayableEventReceived = 8,
    ProviderPayRequestSent = 9,
    ProviderNonPayableEventReceived = 10,
    CdiPassedReceived = 11,
    CdiFailedWithPayReceived = 12,
    CdiFailedWithoutPayReceived = 13,
    OrderRequested = 14
}