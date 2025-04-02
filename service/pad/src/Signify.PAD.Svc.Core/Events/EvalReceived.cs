using NServiceBus;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Messages.Events;

[ExcludeFromCodeCoverage]
public sealed class EvalReceived : IEvent
{
    public Guid Id { get; set; }
    public int EvaluationId { get; set; }
    public int EvaluationTypeId { get; set; }
    public int FormVersionId { get; set; }
    public int? ProviderId { get; set; }
    public string UserName { get; set; }
    public int AppointmentId { get; set; }
    public string ApplicationId { get; set; }
    public int MemberPlanId { get; set; }
    public int MemberId { get; set; }
    public int ClientId { get; set; }
    public string DocumentPath { get; set; }
    public DateTimeOffset CreatedDateTime { get; set; }
    public DateTimeOffset ReceivedDateTime { get; set; }
    public DateTime? DateOfService { get; set; }
}