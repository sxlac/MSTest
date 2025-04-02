using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NServiceBus;

namespace Signify.eGFR.Core.Commands;

[ExcludeFromCodeCoverage]
public class ProviderPayRequest : ICommand
{
    public int ExamId { get; set; }
    public int EvaluationId { get; set; }
    public long ProviderId { get; set; }
    public long MemberPlanId { get; set; }
    public string ProviderProductCode { get; set; }
    public string PersonId { get; set; }
    public string DateOfService { get; set; }
    public int ClientId { get; set; }
    public DateTimeOffset ParentEventDateTime { get; set; }
    public Dictionary<string, string> AdditionalDetails { get; set; }
    public Guid EventId { get; set; }
    public DateTimeOffset ParentEventReceivedDateTime { get; set; }
    
    public string ParentEvent { get; set; }
}