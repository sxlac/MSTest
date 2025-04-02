using Signify.PAD.Svc.System.Tests.Core.Models.Database;

namespace Signify.PAD.Svc.System.Tests.Core.Constants;

public static class ExamStatusCodes
{
    public static readonly PADStatusCode ExamPerformed = new(1, "PADPerformed");
    public static readonly PADStatusCode BillRequestSent = new(2, "BillRequestSent");
    public static readonly PADStatusCode BillableEventReceived = new(3, "BillableEventRecieved");
    public static readonly PADStatusCode ExamNotPerformed = new(4, "PADNotPerformed");
    public static readonly PADStatusCode WaveformDocumentDownloaded = new(5, "WaveformDocumentDownloaded");
    public static readonly PADStatusCode WaveformDocumentUploaded = new(6, "WaveformDocumentUploaded");
    public static readonly PADStatusCode ProviderPayableEventReceived = new(7, "ProviderPayableEventReceived");
    public static readonly PADStatusCode ProviderPayRequestSent = new(8, "ProviderPayRequestSent");
    public static readonly PADStatusCode CdiPassedReceived = new(9, "CdiPassedReceived");
    public static readonly PADStatusCode CdiFailedWithPayReceived = new(10, "CdiFailedWithPayReceived");
    public static readonly PADStatusCode CdiFailedWithoutPayReceived  = new(11, "CdiFailedWithoutPayReceived");
    public static readonly PADStatusCode ProviderNonPayableEventReceived = new(12, "ProviderNonPayableEventReceived");
    public static readonly PADStatusCode BillRequestNotSent = new(13, "BillRequestNotSent");
}


    

    

    

    

    
