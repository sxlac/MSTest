using System;
using System.Collections.Generic;
using System.Linq;

#nullable disable

namespace Signify.DEE.Svc.Core.Data.Entities;

public partial class ExamStatusCode
{
    public enum StatusCodes
    {
        ExamCreated = 1,
        AwaitingInterpreation = 2,
        Interpreted = 3,
        ResultDataDownloaded = 4,
        PdfDataDownloaded = 5,
        SentToBilling = 6,
        NoDeeImagesTaken = 7,
        IrisImageReceived = 8,
        Gradable = 9,
        NotGradable = 10,
        DeeImagesFound = 11,
        IrisExamCreated = 12,
        IrisResultDownloaded = 13,
        PcpLetterSent = 14,
        NoPcpFound = 15,
        MemberLetterSent = 16,
        SentToProviderPay = 17,
        Performed = 18,
        NotPerformed = 19,
        BillableEventRecieved = 20,
        Incomplete = 21,
        BillRequestNotSent = 22,
        ProviderPayableEventReceived = 23,
        ProviderNonPayableEventReceived = 24,
        ProviderPayRequestSent = 25,
        CdiPassedReceived = 26,
        CdiFailedWithPayReceived = 27,
        CdiFailedWithoutPayReceived = 28,
        IrisOrderSubmitted = 29,
        IrisImagesSubmitted = 30
    }

    public static readonly ExamStatusCode ExamCreated = new ExamStatusCode((int)StatusCodes.ExamCreated, "Exam Created");
    public static readonly ExamStatusCode IRISAwaitingInterpretation = new ExamStatusCode((int)StatusCodes.AwaitingInterpreation, "IRIS Awaiting Interpretation");
    public static readonly ExamStatusCode IRISInterpreted = new ExamStatusCode((int)StatusCodes.Interpreted, "IRIS Interpreted");
    public static readonly ExamStatusCode ResultDataDownloaded = new ExamStatusCode((int)StatusCodes.ResultDataDownloaded, "Result Data Downloaded");
    public static readonly ExamStatusCode PDFDataDownloaded = new ExamStatusCode((int)StatusCodes.PdfDataDownloaded, "PDF Data Downloaded");
    public static readonly ExamStatusCode SentToBilling = new ExamStatusCode((int)StatusCodes.SentToBilling, "Sent To Billing");
    public static readonly ExamStatusCode NoDEEImagesTaken = new ExamStatusCode((int)StatusCodes.NoDeeImagesTaken, "No DEE Images Taken");
    public static readonly ExamStatusCode IRISImageReceived = new ExamStatusCode((int)StatusCodes.IrisImageReceived, "IRIS Image Received");
    public static readonly ExamStatusCode Gradable = new ExamStatusCode((int)StatusCodes.Gradable, "Gradable");
    public static readonly ExamStatusCode NotGradable = new ExamStatusCode((int)StatusCodes.NotGradable, "Not Gradable");
    public static readonly ExamStatusCode DEEImagesFound = new ExamStatusCode((int)StatusCodes.DeeImagesFound, "DEE Images Found");
    public static readonly ExamStatusCode IRISExamCreated = new ExamStatusCode((int)StatusCodes.IrisExamCreated, "IRIS Exam Created");
    public static readonly ExamStatusCode IRISResultDownloaded = new ExamStatusCode((int)StatusCodes.IrisResultDownloaded, "IRIS Result Downloaded");
    public static readonly ExamStatusCode PCPLetterSent = new ExamStatusCode((int)StatusCodes.PcpLetterSent, "PCP Letter Sent");
    public static readonly ExamStatusCode NoPCPFound = new ExamStatusCode((int)StatusCodes.NoPcpFound, "No PCP Found");
    public static readonly ExamStatusCode MemberLetterSent = new ExamStatusCode((int)StatusCodes.MemberLetterSent, "Member Letter Sent");
    public static readonly ExamStatusCode SentToProviderPay = new ExamStatusCode((int)StatusCodes.SentToProviderPay, "Sent To Provider Pay");
    public static readonly ExamStatusCode Performed = new ExamStatusCode((int)StatusCodes.Performed, "DEE Performed");
    public static readonly ExamStatusCode NotPerformed = new ExamStatusCode((int)StatusCodes.NotPerformed, "DEE Not Performed");
    public static readonly ExamStatusCode BillableEventRecieved = new ExamStatusCode((int)StatusCodes.BillableEventRecieved, "Billable Event Recieved");
    public static readonly ExamStatusCode Incomplete = new ExamStatusCode((int)StatusCodes.Incomplete, "DEE Incomplete");
    public static readonly ExamStatusCode BillRequestNotSent = new ExamStatusCode((int)StatusCodes.BillRequestNotSent, "Bill Request Not Sent");
    public static readonly ExamStatusCode ProviderPayableEventReceived = new ExamStatusCode((int)StatusCodes.ProviderPayableEventReceived, "ProviderPayableEventReceived");
    public static readonly ExamStatusCode ProviderNonPayableEventReceived = new ExamStatusCode((int)StatusCodes.ProviderNonPayableEventReceived, "ProviderNonPayableEventReceived");
    public static readonly ExamStatusCode ProviderPayRequestSent = new ExamStatusCode((int)StatusCodes.ProviderPayRequestSent, "ProviderPayRequestSent");
    public static readonly ExamStatusCode CdiPassedReceived = new ExamStatusCode((int)StatusCodes.CdiPassedReceived, "CdiPassedReceived");
    public static readonly ExamStatusCode CdiFailedWithPayReceived = new ExamStatusCode((int)StatusCodes.CdiFailedWithPayReceived, "CdiFailedWithPayReceived");
    public static readonly ExamStatusCode CdiFailedWithoutPayReceived = new ExamStatusCode((int)StatusCodes.CdiFailedWithoutPayReceived, "CdiFailedWithoutPayReceived");
    public static readonly ExamStatusCode IrisOrderSubmitted = new ExamStatusCode((int)StatusCodes.IrisOrderSubmitted, "IrisOrderSubmitted");
    public static readonly ExamStatusCode IrisImagesSubmitted = new ExamStatusCode((int)StatusCodes.IrisImagesSubmitted, "IrisImagesSubmitted");

    public static readonly IReadOnlyCollection<ExamStatusCode> All = new List<ExamStatusCode>(new[]
    {
        ExamCreated, IRISAwaitingInterpretation, IRISInterpreted, ResultDataDownloaded,
        PDFDataDownloaded, SentToBilling, NoDEEImagesTaken, IRISImageReceived,
        Gradable, NotGradable, DEEImagesFound, IRISExamCreated, IRISResultDownloaded,
        PCPLetterSent, NoPCPFound, MemberLetterSent, SentToProviderPay, Performed, NotPerformed, BillableEventRecieved, Incomplete, BillRequestNotSent,
        ProviderPayableEventReceived, ProviderNonPayableEventReceived, ProviderPayRequestSent, CdiPassedReceived, CdiFailedWithPayReceived,
        CdiFailedWithoutPayReceived, IrisOrderSubmitted, IrisImagesSubmitted
    });

    public static ExamStatusCode Create(string code)
    {
        return All.SingleOrDefault(x => code.Equals(x.Name, StringComparison.OrdinalIgnoreCase));
    }

    protected internal ExamStatusCode(int id, string name)
    {
        ExamStatusCodeId = id;
        Name = name;
    }

    public ExamStatusCode()
    {
        ExamStatuses = new HashSet<ExamStatus>();
    }

    public int ExamStatusCodeId { get; set; }
    public string Name { get; set; }

    public virtual ICollection<ExamStatus> ExamStatuses { get; set; }
}