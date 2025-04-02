using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.AkkaStreams.Kafka;
using Signify.eGFR.Core.Filters;
using EgfrEvents;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.eGFR.Core.EventHandlers.Akka;

/// <summary>
/// Akka/Kafka event handler for the <see cref="PdfDeliveredToClient"/>
/// </summary>
public class PdfDeliveredToClientHandler(
    ILogger<PdfDeliveredToClientHandler> logger,
    IMessageSession session,
    IProductFilter productFilter)
    : IHandleEvent<PdfDeliveredToClient>
{
    private readonly ILogger _logger = logger;

    [Transaction]
    public async Task Handle(PdfDeliveredToClient @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received PdfDeliveredToClient with {Count} product codes: {ProductCodes}, for EvaluationId={EvaluationId} with EventId={EventId}",
            @event.ProductCodes.Count, string.Join(',', @event.ProductCodes), @event.EvaluationId, @event.EventId);

        if (!productFilter.ShouldProcess(@event.ProductCodes))
        {
            _logger.LogInformation("Event ignored, for EvaluationId={EvaluationId} with EventId={EventId}", @event.EvaluationId, @event.EventId);
            return;
        }

        await session.SendLocal(@event, cancellationToken);

        _logger.LogInformation("Event queued for processing, for EvaluationId={EvaluationId} with EventId={EventId}", @event.EvaluationId, @event.EventId);
    }
}