using NServiceBus;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.Events;

[ExcludeFromCodeCoverage]
public class PADPerformed : IEvent
{
	public Guid CorrelationId { get; set; }

	public int PADId { get; set; }

	public int EvaluationId { get; set; }

	public int MemberPlanId { get; set; }

	public int MemberId { get; set; }

	public int AppointmentId { get; set; }

	public int ProviderId { get; set; }

	public DateTime? DateOfService { get; set; }

	public DateTimeOffset CreatedDateTime { get; set; }

	public DateTimeOffset ReceivedDateTime { get; set; }

	public int ClientId { get; set; }

	public string UserName { get; set; }

	public string ApplicationId { get; set; }
}