namespace Signify.PAD.Svc.Core.Models;

public enum StatusCodes
{
    PadPerformed = 1,
    BillRequestSent = 2,
    BillableEventReceived = 3,
    PadNotPerformed = 4,
    WaveformDocumentDownloaded = 5,
    WaveformDocumentUploaded = 6,
    ProviderPayableEventReceived = 7,
    ProviderPayRequestSent = 8,
    CdiPassedReceived = 9,
    CdiFailedWithPayReceived = 10,
    CdiFailedWithoutPayReceived = 11,
    ProviderNonPayableEventReceived = 12,
    BillRequestNotSent = 13
}
