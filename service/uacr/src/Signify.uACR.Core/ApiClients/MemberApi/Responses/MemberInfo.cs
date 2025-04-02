using System.Diagnostics.CodeAnalysis;
using System;

namespace Signify.uACR.Core.ApiClients.MemberApi.Responses;

/// <summary>
/// Details of a member (patient) that an evaluation was performed on. This is a subset of the full member model,
/// which can be found at
/// https://chgit.censeohealth.com/projects/MEM/repos/memberapi/browse/src/CH.Member.WebApi/Representations/MemberRepresentation.cs
/// </summary>
[ExcludeFromCodeCoverage]
public class MemberInfo
{
    public long MemberId { get; set; }
    public string CenseoId { get; set; }
    public string FirstName { get; set; }
    public string MiddleName { get; set; }
    public string LastName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string AddressLineOne { get; set; }
    public string AddressLineTwo { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string ZipCode { get; set; }

    /// <summary>
    /// Client this member belongs to
    /// </summary>
    public string Client { get; set; }
}