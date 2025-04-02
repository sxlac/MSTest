using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.AkkaStreams.Kafka;
using Signify.FOBT.Svc.Core.Events;
using Signify.FOBT.Svc.Core.Filters;
using Signify.FOBT.Svc.Core.Infrastructure;

namespace Signify.FOBT.Svc.Core.EventHandlers.Akka;

/// <summary>
/// Handle CDIFailedEvent. If EvaluationId has the PM's product code,
/// raise a NSB command to handle ProviderPay.
/// </summary>
public class CdiPassedEventHandler : CdiEventHandlerBase<CDIPassedEvent, CdiPassedEventHandler>, IHandleEvent<CDIPassedEvent>
{
    public CdiPassedEventHandler(ILogger<CdiPassedEventHandler> logger, IMessageSession messageSession, IProductFilter productFilter, IApplicationTime applicationTime)
        : base(logger, messageSession, productFilter, applicationTime)
    {
    }

    [Transaction]
    public async Task Handle(CDIPassedEvent @event, CancellationToken cancellationToken)
    {
        await ValidateAndRaiseNsbCommand(@event, cancellationToken);
    }
}