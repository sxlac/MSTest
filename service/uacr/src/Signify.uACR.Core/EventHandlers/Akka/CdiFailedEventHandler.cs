using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.AkkaStreams.Kafka;
using System.Threading.Tasks;
using System.Threading;
using Signify.uACR.Core.Events.Akka;
using Signify.uACR.Core.FeatureFlagging;
using Signify.uACR.Core.Filters;
using Signify.uACR.Core.Infrastructure;

namespace Signify.uACR.Core.EventHandlers.Akka;

/// <summary>
/// Handle CDIFailedEvent. If EvaluationId has the PM's product code,
/// raise a NSB command to handle ProviderPay.
/// </summary>
public class CdiFailedEventHandler(
    ILogger<CdiFailedEventHandler> logger,
    IMessageSession messageSession,
    IFeatureFlags featureFlags,
    IProductFilter productFilter,
    IApplicationTime applicationTime)
    : CdiEventHandlerBase<CDIFailedEvent, CdiFailedEventHandler>(logger, messageSession, featureFlags, productFilter,
        applicationTime), IHandleEvent<CDIFailedEvent>
{
    [Transaction]
    public async Task Handle(CDIFailedEvent @event, CancellationToken cancellationToken)
    {
        await ValidateAndRaiseNsbCommand(@event, cancellationToken);
    }
}