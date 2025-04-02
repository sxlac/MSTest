using System;
using System.Diagnostics.CodeAnalysis;
using MediatR;
using Signify.FOBT.Messages.Events.Status;

namespace Signify.FOBT.Svc.Core.Events.Status;

[ExcludeFromCodeCoverage]
public class ProviderPayRequestSent : BaseStatusMessage, IRequest<bool>
{
    public string ProviderPayProductCode { get; set; }
    public string PaymentId { get; set; }
    public DateTimeOffset ParentEventDateTime { get; set; }
}