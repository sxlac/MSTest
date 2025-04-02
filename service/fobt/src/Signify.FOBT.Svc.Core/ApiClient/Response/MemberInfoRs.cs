using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Signify.FOBT.Svc.Core.ApiClient.Response;

[ExcludeFromCodeCoverage]
public class MemberInfoRs
{
    public string FirstName { get; set; }
    public object MiddleName { get; set; }
    public string LastName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string AddressLineOne { get; set; }
    public string AddressLineTwo { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string ZipCode { get; set; }
    public string Client { get; set; }
    public string CenseoId { get; set; }
    public string MemberId { get; set; }
    public string Gender { get; set; }
    public string PlanId { get; set; }
    public string SubscriberId { get; set; }
    public List<MemberPhones> MemberPhones { get; set; }
} 

[ExcludeFromCodeCoverage]
public class MemberPhones     
{
  public int MemberPhoneId { get; set; }
  public string PhoneNumber { get; set; }
}