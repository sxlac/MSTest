using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.AkkaStreams.Kafka;
using Signify.DEE.Svc.Core.Events;
using Signify.DEE.Svc.Core.FeatureFlagging;
using Signify.DEE.Svc.Core.Filters;
using Signify.DEE.Svc.Core.Infrastructure;

namespace Signify.DEE.Svc.Core.EventHandlers.Akka;

/// <summary>
/// Handle CDIFailedEvent. If EvaluationId has the PM's product code,
/// raise a NSB command to handle ProviderPay.
/// </summary>
public class CdiPassedEventHandler(
    ILogger<CdiPassedEventHandler> logger,
    IMessageSession messageSession,
    IFeatureFlags featureFlags,
    IProductFilter productFilter,
    IApplicationTime applicationTime)
    : CdiEventHandlerBase<CDIPassedEvent, CdiPassedEventHandler>(logger, messageSession, featureFlags, productFilter,
        applicationTime), IHandleEvent<CDIPassedEvent>
{
    [Transaction]
    public async Task Handle(CDIPassedEvent @event, CancellationToken cancellationToken)
    {
        await ValidateAndRaiseNsbCommand(@event, cancellationToken);
    }
}