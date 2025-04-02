using Microsoft.Extensions.Logging;
using NServiceBus;
using Signify.AkkaStreams.Kafka;
using Signify.Spirometry.Core.Events;
using Signify.Spirometry.Core.Filters;
using Signify.Spirometry.Core.Infrastructure;

namespace Signify.Spirometry.Core.EventHandlers.Akka;

/// <summary>
/// Handle CDIPassedEvent and pass the event to base Handle method to filter by Product Code and
/// raise a NSB command to handle ProviderPay
/// </summary>
public class CdiPassedEventHandler : CdiEventHandlerBase<CDIPassedEvent>, IHandleEvent<CDIPassedEvent>
{
    public CdiPassedEventHandler(ILogger<CdiPassedEventHandler> logger, IMessageSession messageSession, IProductFilter productFilter, IApplicationTime applicationTime)
        : base(logger, messageSession, productFilter, applicationTime)
    {
    }
}