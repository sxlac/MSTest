using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.eGFR.Core.Events.Akka;

namespace Signify.eGFR.Core.Commands;

/// <summary>
/// Publish to Kafka an OrderCreationEvent 
/// </summary>
[ExcludeFromCodeCoverage]
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
    private readonly ILogger _logger = logger;

    /// <summary>
    /// Handler to publish to Kafka an OrderCreationEvent 
    /// </summary>
    public async Task Handle(PublishOrderCreation request, CancellationToken cancellationToken)
    {
        await messageProducer.Produce(request.EvaluationId.ToString(), request.Event, cancellationToken);

        if (request.EventId != Guid.Empty)
            _logger.LogInformation(
                "Enqueued publishing results to Kafka, for EvaluationId={EvaluationId}, EventId={EventId}",
                request.EvaluationId, request.EventId);
        else
            _logger.LogInformation("Enqueued publishing results to Kafka, for EvaluationId={EvaluationId}",
                request.EvaluationId);
    }
}