using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.Spirometry.Core.Events;
using Signify.Spirometry.Core.Filters;
using Signify.Spirometry.Core.Infrastructure;

namespace Signify.Spirometry.Core.EventHandlers.Akka;

/// <summary>
/// Base class for handling cdi_events.
/// Checks if the PM's product code is present, and raises a NSB command.
/// </summary>
public abstract class CdiEventHandlerBase<TEvent>
    where TEvent : CdiEventBase
{
    private readonly IMessageSession _messageSession;
    private readonly ILogger _logger;
    private readonly IProductFilter _productFilter;
    private readonly IApplicationTime _applicationTime;

    protected CdiEventHandlerBase(ILogger logger, IMessageSession messageSession,
        IProductFilter productFilter, IApplicationTime applicationTime)
    {
        _messageSession = messageSession;
        _logger = logger;
        _productFilter = productFilter;
        _applicationTime = applicationTime;
    }

    [Transaction]
    public virtual async Task Handle(TEvent cdiEvent, CancellationToken cancellationToken)
    {
        var eventType = typeof(TEvent).Name;
        _logger.LogInformation("Received {Event} with {Count} product codes: {ProductCodes}, for EvaluationId={EvaluationId} with RequestId={RequestId}",
            eventType, cdiEvent.Products?.Count, GetProductCodes(cdiEvent.Products), cdiEvent.EvaluationId, cdiEvent.RequestId);

        if (!_productFilter.ShouldProcess(cdiEvent.Products))
        {
            _logger.LogDebug("Event ignored for EvaluationId={EvaluationId}, RequestId={RequestId}", cdiEvent.EvaluationId, cdiEvent.RequestId);
            return;
        }

        cdiEvent.ReceivedBySpiroDateTime = _applicationTime.UtcNow();
        await _messageSession.SendLocal(cdiEvent);

        _logger.LogInformation("{Event} queued for processing payment, for EvaluationId={EvaluationId} with EventId={RequestId}",
            eventType, cdiEvent.EvaluationId, cdiEvent.RequestId);
    }

    /// <summary>
    /// Gets a comma separated list of product codes
    /// </summary>
    /// <param name="products"></param>
    /// <returns></returns>
    private static string GetProductCodes(ICollection<DpsProduct> products)
    {
        return products == null ? string.Empty : string.Join(',', products.Select(p => p.ProductCode));
    }
}