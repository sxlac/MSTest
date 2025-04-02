using Microsoft.Extensions.Logging;
using NServiceBus;
using System.Threading;
using System.Threading.Tasks;
using NewRelic.Api.Agent;
using Signify.AkkaStreams.Kafka;
using Signify.HBA1CPOC.Messages.Events.Akka;

namespace Signify.HBA1CPOC.Svc.Core.EventHandlers.Akka;

public class CdiPassedEventHandler : CdiEventHandlerBase<CDIPassedEvent, CdiPassedEventHandler>, IHandleEvent<CDIPassedEvent>
{
    public CdiPassedEventHandler(ILogger<CdiPassedEventHandler> logger, IMessageSession messageSession)
        : base(logger, messageSession)
    {
    }

    [Transaction]
    public async Task Handle(CDIPassedEvent @event, CancellationToken cancellationToken)
    {
        await ValidateAndRaiseNsbCommand(@event, cancellationToken);
    }
}
