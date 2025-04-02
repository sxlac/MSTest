using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.Models;

[Obsolete("To be removed in ANC-3978; please publish events that derive from Events.Status.BaseStatusMessage")]
[ExcludeFromCodeCoverage]
public abstract class PadStatusCode
{
    public string ProductCode { get; set; }
    public int? EvaluationId { get; set; }
    public long? MemberPlanId { get; set; }
    public int? ProviderId { get; set; }
    public DateTimeOffset CreateDate { get; set; }
    public DateTimeOffset ReceivedDate { get; set; }
}