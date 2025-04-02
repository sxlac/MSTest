using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Signify.uACR.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public class Exam
{
    public Exam()
    {
        BarcodeExams = new HashSet<BarcodeExam>();
        BillRequests = new HashSet<BillRequest>();
        ExamStatuses = new HashSet<ExamStatus>();
    }

    public int ExamId { get; set; }
    public long EvaluationId { get; set; }
    public string ApplicationId { get; set; }
    public int ProviderId { get; set; }
    public long MemberId { get; set; }
    public long MemberPlanId { get; set; }
    public string CenseoId { get; set; }
    public long AppointmentId { get; set; }
    public int ClientId { get; set; }
    public DateTimeOffset? DateOfService { get; set; }
    public string FirstName { get; set; }
    public string MiddleName { get; set; }
    public string LastName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string AddressLineOne { get; set; }
    public string AddressLineTwo { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string ZipCode { get; set; }
    public string NationalProviderIdentifier { get; set; }
    public DateTimeOffset EvaluationReceivedDateTime { get; set; }
    public DateTimeOffset EvaluationCreatedDateTime { get; set; }
    public DateTimeOffset CreatedDateTime { get; set; }

    public virtual ExamNotPerformed ExamNotPerformed { get; set; }
    public virtual ProviderPay ProviderPay { get; set; }
    public virtual ICollection<BarcodeExam> BarcodeExams { get; set; }
    public virtual ICollection<BillRequest> BillRequests { get; set; }
    public virtual ICollection<ExamStatus> ExamStatuses { get; set; }
}