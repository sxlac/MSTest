using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.AkkaStreams.Kafka;
using Signify.PAD.Svc.Core.Events;
using Signify.PAD.Svc.Core.Filters;

namespace Signify.PAD.Svc.Core.EventHandlers.Akka;

/// <summary>
/// Handle CDIFailedEvent. If EvaluationId has the PM's product code,
/// raise a NSB command to handle ProviderPay.
/// </summary>
public class CdiFailedEventHandler : CdiEventHandlerBase<CDIFailedEvent, CdiFailedEventHandler>, IHandleEvent<CDIFailedEvent>
{
    public CdiFailedEventHandler(ILogger<CdiFailedEventHandler> logger, IMessageSession messageSession, IProductFilter productFilter)
        : base(logger, messageSession, productFilter)
    {
    }

    [Transaction]
    public async Task Handle(CDIFailedEvent @event, CancellationToken cancellationToken)
    {
        await ValidateAndRaiseNsbCommand(@event, cancellationToken);
    }
}