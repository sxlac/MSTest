using Signify.uACR.System.Tests.Core.Models.Database;

namespace Signify.uACR.System.Tests.Core.Constants;

public static class ExamStatusCodes
{
    public static readonly StatusCode ExamPerformed = new() { ExamStatusCodeId = 1, StatusName = "Exam Performed" };
    public static readonly StatusCode ExamNotPerformed = new() { ExamStatusCodeId = 2, StatusName = "Exam Not Performed"};
    public static readonly StatusCode BillableEventReceived = new(){ ExamStatusCodeId = 3, StatusName = "Billable Event Received"};
    public static readonly StatusCode BillRequestSent = new(){ ExamStatusCodeId = 4, StatusName = "Bill Request Sent"};
    public static readonly StatusCode ClientPdfDelivered = new(){ ExamStatusCodeId = 5, StatusName = "Client PDF Delivered"};
    public static readonly StatusCode LabResultsReceived = new(){ ExamStatusCodeId = 6, StatusName = "Lab Results Received"};
    public static readonly StatusCode BillRequestNotSent = new(){ ExamStatusCodeId = 7, StatusName = "Bill Request Not Sent"};
    public static readonly StatusCode ProviderPayableEventReceived = new(){ ExamStatusCodeId = 8, StatusName = "ProviderPayableEventReceived"};
    public static readonly StatusCode ProviderPayRequestSent = new(){ ExamStatusCodeId = 9, StatusName = "ProviderPayRequestSent"};
    public static readonly StatusCode ProviderNonPayableEventReceived = new(){ ExamStatusCodeId = 10, StatusName = "ProviderNonPayableEventReceived"};
    public static readonly StatusCode CdiPassedReceived = new(){ ExamStatusCodeId = 11, StatusName = "CDIPassedReceived"};
    public static readonly StatusCode CdiFailedWithPayReceived = new(){ ExamStatusCodeId = 12, StatusName = "CDIFailedWithPayReceived"};
    public static readonly StatusCode CdiFailedWithoutPayReceived = new(){ ExamStatusCodeId = 13, StatusName = "CDIFailedWithoutPayReceived"};
    public static readonly StatusCode OrderRequested = new(){ ExamStatusCodeId = 14, StatusName = "Order Requested"};
}