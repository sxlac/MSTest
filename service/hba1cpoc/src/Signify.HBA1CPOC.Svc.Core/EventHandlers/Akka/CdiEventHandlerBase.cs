using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.AkkaStreams.Kafka;
using Signify.HBA1CPOC.Messages.Events;

namespace Signify.HBA1CPOC.Svc.Core.EventHandlers.Akka;

/// <summary>
/// Base class for handling cdi_events.
/// Checks if the PM's product code is present, and raises a NSB command.
/// </summary>
public abstract class CdiEventHandlerBase<TE, TH>
    where TE : BaseCdiEvent
    where TH : IHandleEvent<TE>
{
    private readonly IMessageSession _messageSession;
    private readonly ILogger<TH> _logger;

    protected CdiEventHandlerBase(ILogger<TH> logger, IMessageSession messageSession)
    {
        _messageSession = messageSession;
        _logger = logger;
    }

    [Transaction]
    protected async Task ValidateAndRaiseNsbCommand(TE cdiEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received {Event} with {Count} product codes: {ProductCodes}, for EvaluationId={EvaluationId} with RequestId={RequestId}",
            typeof(TE), cdiEvent.Products?.Count, GetProductCodes(cdiEvent.Products), cdiEvent.EvaluationId, cdiEvent.RequestId);
        
        var shouldProcess = cdiEvent.Products?.Any(p =>
            string.Equals(p.ProductCode, Constants.ApplicationConstants.ProductCode, StringComparison.OrdinalIgnoreCase));
        
        if (shouldProcess != true)
        {
            _logger.LogInformation("CdiPassed event ignored, for EvaluationId={EvaluationId} with EventId={RequestId}", @cdiEvent.EvaluationId, @cdiEvent.RequestId);
            return;
        }

        _logger.LogInformation("Evaluation Identified with product code HBA1CPOC, EvaluationID : {EvaluationId}, RequestId: {RequestId}",
            cdiEvent.EvaluationId, cdiEvent.RequestId);

        await _messageSession.SendLocal(cdiEvent);

        _logger.LogInformation("Event queued for processing, for EvaluationId={EvaluationId} with EventId={RequestId}. End of {Handle}",
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