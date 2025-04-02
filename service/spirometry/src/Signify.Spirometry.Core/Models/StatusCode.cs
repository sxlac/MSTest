namespace Signify.Spirometry.Core.Models
{
    public enum StatusCode
    {
        SpirometryExamPerformed = 1,
        SpirometryExamNotPerformed = 2,
        BillableEventReceived = 3,
        BillRequestSent = 4,
        ClientPdfDelivered = 5,
        BillRequestNotSent = 6,
        OverreadProcessed = 7,
        ResultsReceived = 8,
        ClarificationFlagCreated = 9,
        ProviderPayableEventReceived = 10,
        ProviderPayRequestSent = 11,
        ProviderNonPayableEventReceived = 12,
        CdiPassedReceived = 13,
        CdiFailedWithPayReceived = 14,
        CdiFailedWithoutPayReceived = 15
    }
}
