using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NServiceBus;

namespace Signify.HBA1CPOC.Svc.Core.Commands;

[ExcludeFromCodeCoverage]
public class ProviderPayRequest : ICommand
{
    public Guid EventId { get; set; }
    public int HBA1CPOCId { get; set; }
    public int EvaluationId { get; set; }
    public long ProviderId { get; set; }
    public string ProviderProductCode { get; set; }
    public string PersonId { get; set; }
    public string DateOfService { get; set; }
    public int ClientId { get; set; }
    public DateTime PdfDeliveryDateTime { get; set; }
    public Dictionary<string, string> AdditionalDetails { get; set; }
}