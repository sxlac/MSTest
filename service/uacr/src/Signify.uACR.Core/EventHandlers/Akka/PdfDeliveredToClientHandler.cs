using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.AkkaStreams.Kafka;
using Signify.uACR.Core.Filters;
using UacrEvents;

namespace Signify.uACR.Core.EventHandlers.Akka;

public class PdfDeliveredToClientHandler(
    ILogger<PdfDeliveredToClientHandler> logger,
    IMessageSession messageSession,
    IProductFilter productFilter)
    : IHandleEvent<PdfDeliveredToClient>
{
    [Transaction]
    public async Task Handle(PdfDeliveredToClient @event, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received PdfDeliveredToClient with {Count} product codes: {ProductCodes}, for EvaluationId={EvaluationId} with EventId={EventId}",
            @event.ProductCodes.Count, string.Join(',', @event.ProductCodes), @event.EvaluationId, @event.EventId);

        if (!productFilter.ShouldProcess(@event.ProductCodes))
        {
            logger.LogInformation("Event ignored, for EvaluationId={EvaluationId} with EventId={EventId}", @event.EvaluationId, @event.EventId);
            return;
        }

        await messageSession.SendLocal(@event, cancellationToken: cancellationToken);

        logger.LogInformation("Event queued for processing, for EvaluationId={EvaluationId} with EventId={EventId}", @event.EvaluationId, @event.EventId);
    }
}