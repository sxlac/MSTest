using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NServiceBus;

namespace Signify.PAD.Svc.Core.Commands;

[ExcludeFromCodeCoverage]
public class ProviderPayRequest : ICommand
{
    public int PadId { get; set; }
    public int EvaluationId { get; set; }
    public long ProviderId { get; set; }
    public string ProviderProductCode { get; set; }
    public string PersonId { get; set; }
    public string DateOfService { get; set; }
    public int ClientId { get; set; }
    public DateTime PdfDeliveryDateTime { get; set; }
    public Dictionary<string, string> AdditionalDetails { get; set; }
}