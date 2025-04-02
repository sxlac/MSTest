using System;
using System.Collections.Generic;
using NServiceBus;

namespace Signify.FOBT.Svc.Core.Commands;

public class ProviderPayRequest : ICommand
{
    public int ExamId { get; set; }
    public int EvaluationId { get; set; }
    public long ProviderId { get; set; }
    public string ProviderProductCode { get; set; }
    public string PersonId { get; set; }
    public string DateOfService { get; set; }
    public int ClientId { get; set; }
    public Dictionary<string, string> AdditionalDetails { get; set; }
    public Guid EventId { get; set; }

    /// <summary>
    /// Date and time contained within the Kafka event that triggered this NSB
    /// </summary>
    public DateTimeOffset ParentEventDateTime { get; set; }

    /// <summary>
    /// Date and time the Kafka event that triggered this Nsb was received by PM
    /// </summary>
    public DateTimeOffset ParentEventReceivedDateTime { get; set; }

    public string ParentEvent { get; set; }
}