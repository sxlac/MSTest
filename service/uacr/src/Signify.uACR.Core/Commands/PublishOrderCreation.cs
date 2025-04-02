using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.uACR.Core.Events.Akka;

namespace Signify.uACR.Core.Commands;

/// <summary>
/// Publish to Kafka an OrderCreationEvent 
/// </summary>
public class PublishOrderCreation(OrderCreationEvent @event, Guid eventId) : IRequest
{
    public Guid EventId { get; } = eventId;
    public OrderCreationEvent Event { get; } = @event;
    public long EvaluationId => Event.EvaluationId;

    public PublishOrderCreation(OrderCreationEvent @event)
        : this(@event, Guid.Empty)
    {
    }
}

public class PublishOrderCreationHandler(ILogger<PublishOrderCreationHandler> logger, IMessageProducer messageProducer)
    : IRequestHandler<PublishOrderCreation>
{
    /// <summary>
    /// Handler to publish to Kafka an OrderCreationEvent 
    /// </summary>
    public async Task Handle(PublishOrderCreation request, CancellationToken cancellationToken)
    {
        await messageProducer.Produce(request.EvaluationId.ToString(), request.Event, cancellationToken);

        if (request.EventId != Guid.Empty)
            logger.LogInformation(
                "Enqueued publishing results to Kafka, for EvaluationId={EvaluationId}, EventId={EventId}",
                request.EvaluationId, request.EventId);
        else
            logger.LogInformation("Enqueued publishing results to Kafka, for EvaluationId={EvaluationId}",
                request.EvaluationId);
    }
}