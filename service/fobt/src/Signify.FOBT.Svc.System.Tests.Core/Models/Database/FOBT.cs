namespace Signify.FOBT.Svc.System.Tests.Core.Models.Database;

public class FOBT
{
    public int FOBTId { get; set; }
    public int EvaluationId { get; set; }
    public int MemberPlanId { get; set; }
    public int MemberId { get; set; }
    public string CenseoId { get; set; }
    public int AppointmentId { get; set; }
    public int ProviderId { get; set; }
    public DateTime DateOfService { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public DateTime ReceivedDateTime { get; set; }
    public string Barcode { get; set; }
    public int ClientId { get; set; }
    public string UserName { get; set; }
    public string ApplicationId { get; set; }
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
    public Guid OrderCorrelationId { get; set; }
    
    
    
}