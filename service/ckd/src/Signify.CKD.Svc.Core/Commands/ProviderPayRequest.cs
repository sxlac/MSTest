using System;
using System.Collections.Generic;
using NServiceBus;

namespace Signify.CKD.Svc.Core.Commands;

public class ProviderPayRequest : ICommand
{
    public int CkdId { get; set; }
    public int EvaluationId { get; set; }
    public long ProviderId { get; set; }
    public string ProviderProductCode { get; set; }
    public string PersonId { get; set; }
    public string DateOfService { get; set; }
    public int ClientId { get; set; }
    public DateTime PdfDeliveryDateTime { get; set; }
    public Dictionary<string, string> AdditionalDetails { get; set; }
}