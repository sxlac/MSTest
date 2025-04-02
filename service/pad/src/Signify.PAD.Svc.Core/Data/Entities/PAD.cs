using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public class PAD
{
	public int PADId { get; set; }
	public int? EvaluationId { get; set; }
	public int? MemberPlanId { get; set; }
	public int? MemberId { get; set; }
	public string CenseoId { get; set; }
	public int? AppointmentId { get; set; }
	public int? ProviderId { get; set; }
	public DateTime? DateOfService { get; set; }
	public DateTimeOffset CreatedDateTime { get; set; }
	public DateTimeOffset ReceivedDateTime { get; set; }
	public int? ClientId { get; set; }
	public string UserName { get; set; }
	public string ApplicationId { get; set; }
	public string FirstName { get; set; }
	public string MiddleName { get; set; }
	public string LastName { get; set; }
	public DateTime? DateOfBirth { get; set; }
	public string AddressLineOne { get; set; }
	public string AddressLineTwo { get; set; }
	public string City { get; set; }
	public string State { get; set; }
	public string ZipCode { get; set; }
	public string NationalProviderIdentifier { get; set; }
	public string LeftScoreAnswerValue { get; set; }
	public string LeftSeverityAnswerValue { get; set; }
	public string RightScoreAnswerValue { get; set; }
	public string RightSeverityAnswerValue { get; set; }
	public string LeftNormalityIndicator { get; set; }
	public string RightNormalityIndicator { get; set; }
}