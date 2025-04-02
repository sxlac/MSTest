namespace Signify.eGFR.System.Tests.Core.Models.Database;

public class Exam
{
    public int ExamId { get; set; }
    public long EvaluationId { get; set; }
    public string ApplicationId { get; set; }
    public int ProviderId { get; set; }
    public long MemberId { get; set; }
    public long MemberPlanId { get; set; }
    public string CenseoId { get; set; }
    public long AppointmentId { get; set; }
    public int ClientId { get; set; }
    public DateTime DateOfService { get; set; }
    public string FirstName { get; set; }
    public string MiddleName { get; set; }
    public string LastName { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string AddressLineOne { get; set; }
    public string AddressLineTwo { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string ZipCode { get; set; }
    public string NationalProviderIdentifier { get; set; }
    public DateTime EvaluationReceivedDateTime { get; set; }
    public DateTime EvaluationCreationDateTime { get; set; }
    public DateTime CreatedDateTime { get; set; }
}