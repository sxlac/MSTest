using Microsoft.Extensions.Logging;
using NServiceBus;
using System.Threading;
using System.Threading.Tasks;
using NewRelic.Api.Agent;
using Signify.AkkaStreams.Kafka;
using Signify.HBA1CPOC.Messages.Events.Akka;

namespace Signify.HBA1CPOC.Svc.Core.EventHandlers.Akka;

public class CdiFailedEventHandler : CdiEventHandlerBase<CDIFailedEvent, CdiFailedEventHandler>, IHandleEvent<CDIFailedEvent>
{
    public CdiFailedEventHandler(ILogger<CdiFailedEventHandler> logger, IMessageSession messageSession)
        : base(logger, messageSession)
    {
    }

    [Transaction]
    public async Task Handle(CDIFailedEvent @event, CancellationToken cancellationToken)
    {
        await ValidateAndRaiseNsbCommand(@event, cancellationToken);
    }
}

