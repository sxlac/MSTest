using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.uACR.Core.Events.Status;

namespace Signify.uACR.Core.Commands;

/// <summary>
/// Command to publish a status update event
/// </summary>
public class PublishStatusUpdate(Guid eventId, BaseStatusMessage status) : IRequest
{
    public Guid EventId { get; } = eventId;

    /// <summary>
    /// Status message to publish
    /// </summary>
    public BaseStatusMessage Status { get; } = status;
}

public class PublishStatusUpdateHandler(ILogger<PublishStatusUpdateHandler> logger, IMessageProducer messageProducer)
    : IRequestHandler<PublishStatusUpdate>
{
    public async Task Handle(PublishStatusUpdate request, CancellationToken cancellationToken)
    {
        await messageProducer.Produce(request.Status.EvaluationId.ToString(), request.Status, cancellationToken);

        logger.LogInformation("Published EventType={EventType} status update, for EvaluationId={EvaluationId} with EventId={EventId}",
            request.Status.GetType().Name, request.Status.EvaluationId, request.EventId);
    }
}