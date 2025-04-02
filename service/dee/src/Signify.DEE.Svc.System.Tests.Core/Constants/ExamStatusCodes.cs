using Signify.DEE.Svc.System.Tests.Core.Models.Database;

namespace Signify.DEE.Svc.System.Tests.Core.Constants;

public static class ExamStatusCodes
{
    public static readonly StatusCode ExamCreated = new() { ExamStatusCodeId = 1, Name = "Exam Created" };
    public static readonly StatusCode AwaitingInterpretation = new() { ExamStatusCodeId = 2, Name = "IRIS Awaiting Interpretation"};
    public static readonly StatusCode IrisInterpreted = new(){ ExamStatusCodeId = 3, Name = "IRIS Interpreted"};
    public static readonly StatusCode ResultDataDownloaded = new(){ ExamStatusCodeId = 4, Name = "Result Data Downloaded"};
    public static readonly StatusCode PdfDataDownloaded = new(){ ExamStatusCodeId = 5, Name = "PDF Data Downloaded"};
    public static readonly StatusCode SentToBilling = new(){ ExamStatusCodeId = 6, Name = "Sent To Billing"};
    public static readonly StatusCode NoDeeImagesTaken = new(){ ExamStatusCodeId = 7, Name = "No DEE Images Taken"};
    public static readonly StatusCode IrisImageReceived = new(){ ExamStatusCodeId = 8, Name = "IRIS Image Received"};
    public static readonly StatusCode Gradable = new(){ ExamStatusCodeId = 9, Name = "Gradable"};
    public static readonly StatusCode NotGradable = new(){ ExamStatusCodeId = 10, Name = "Not Gradable"};
    public static readonly StatusCode DeeImagesFound = new(){ ExamStatusCodeId = 11, Name = "DEE Images Found"};
    public static readonly StatusCode IrisExamCreated = new(){ ExamStatusCodeId = 12, Name = "IRIS Exam Created"};
    public static readonly StatusCode IrisResultDownloaded = new(){ ExamStatusCodeId = 13, Name = "IRIS Result Downloaded"};
    public static readonly StatusCode PcpLetterSent = new(){ ExamStatusCodeId = 14, Name = "PCP Letter Sent"};
    public static readonly StatusCode NoPcpFound = new(){ ExamStatusCodeId = 15, Name = "No PCP Found"};
    public static readonly StatusCode MemberLetterSent = new(){ ExamStatusCodeId = 16, Name = "Member Letter Sent"};
    public static readonly StatusCode SentToProviderPay = new(){ ExamStatusCodeId = 17, Name = "Sent To Provider Pay"};
    public static readonly StatusCode DeePerformed = new(){ ExamStatusCodeId = 18, Name = "DEE Performed"};
    public static readonly StatusCode DeeNotPerformed = new(){ ExamStatusCodeId = 19, Name = "DEE Not Performed"};
    public static readonly StatusCode BillableEventRecieved = new(){ ExamStatusCodeId = 20, Name = "Billable Event Recieved"};
    public static readonly StatusCode DeeIncomplete = new(){ ExamStatusCodeId = 21, Name = "DEE Incomplete"};
    public static readonly StatusCode BillRequestNotSent = new(){ ExamStatusCodeId = 22, Name = "Bill Request Not Sent"};
    public static readonly StatusCode ProviderPayableEventReceived = new(){ ExamStatusCodeId = 23, Name = "ProviderPayableEventReceived"};
    public static readonly StatusCode ProviderNonPayableEventReceived = new(){ ExamStatusCodeId = 24, Name = "ProviderNonPayableEventReceived"};
    public static readonly StatusCode ProviderPayRequestSent = new(){ ExamStatusCodeId = 25, Name = "ProviderPayRequestSent"};
    public static readonly StatusCode CdiPassedReceived = new(){ ExamStatusCodeId = 26, Name = "CdiPassedReceived"};
    public static readonly StatusCode CdiFailedWithPayReceived = new(){ ExamStatusCodeId = 27, Name = "CdiFailedWithPayReceived"};
    public static readonly StatusCode CdiFailedWithoutPayReceived = new(){ ExamStatusCodeId = 28, Name = "CdiFailedWithoutPayReceived"};
    public static readonly StatusCode IrisOrderSubmitted = new(){ ExamStatusCodeId = 29, Name = "IrisOrderSubmitted"};
    public static readonly StatusCode IrisImagesSubmitted = new(){ ExamStatusCodeId = 30, Name = "IrisImagesSubmitted"};
}


