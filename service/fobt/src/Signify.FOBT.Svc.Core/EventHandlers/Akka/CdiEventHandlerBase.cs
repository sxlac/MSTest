using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.AkkaStreams.Kafka;
using Signify.FOBT.Svc.Core.Constants;
using Signify.FOBT.Svc.Core.Events;
using Signify.FOBT.Svc.Core.Filters;
using Signify.FOBT.Svc.Core.Infrastructure;

namespace Signify.FOBT.Svc.Core.EventHandlers.Akka;

/// <summary>
/// Base class for handling cdi_events.
/// Checks if the PM's product code is present, and raises a NSB command.
/// </summary>
public abstract class CdiEventHandlerBase<TE, TH>
    where TE : CdiEventBase
    where TH : IHandleEvent<TE>
{
    private readonly IMessageSession _messageSession;
    private readonly ILogger<TH> _logger;
    private readonly IProductFilter _productFilter;
    private readonly IApplicationTime _applicationTime;

    protected CdiEventHandlerBase(ILogger<TH> logger, IMessageSession messageSession, IProductFilter productFilter, IApplicationTime applicationTime)
    {
        _messageSession = messageSession;
        _logger = logger;
        _productFilter = productFilter;
        _applicationTime = applicationTime;
    }

    [Transaction]
    protected async Task ValidateAndRaiseNsbCommand(TE cdiEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received {Event} with {Count} product codes: {ProductCodes}, for EvaluationId={EvaluationId} with RequestId={RequestId}",
            typeof(TE), cdiEvent.Products?.Count, GetProductCodes(cdiEvent.Products), cdiEvent.EvaluationId,
            cdiEvent.RequestId);

        if (!_productFilter.ShouldProcess(cdiEvent.Products))
        {
            _logger.LogInformation(
                "Evaluation ignored as product code is not present, EvaluationID:{EvaluationId}, RequestId: {RequestId}",
                cdiEvent.EvaluationId, cdiEvent.RequestId);
            return;
        }

        _logger.LogInformation(
            "Evaluation Identified with product code {ProductCode}, EvaluationID : {EvaluationId}, RequestId: {RequestId}",
            ApplicationConstants.PRODUCT_CODE, cdiEvent.EvaluationId, cdiEvent.RequestId);

        cdiEvent.ReceivedByFobtDateTime = _applicationTime.UtcNow();
        await _messageSession.SendLocal(cdiEvent, cancellationToken);

        _logger.LogInformation(
            "Event queued for processing, for EvaluationId={EvaluationId} with EventId={RequestId}. End of {Handle}",
            cdiEvent.EvaluationId, cdiEvent.RequestId, typeof(TH));
    }

    /// <summary>
    /// Gets a comma separated list of product codes
    /// </summary>
    /// <param name="products"></param>
    /// <returns></returns>
    private static string GetProductCodes(IReadOnlyCollection<DpsProduct> products)
    {
        return products == null ? string.Empty : string.Join(',', products.Select(p => p.ProductCode));
    }
}