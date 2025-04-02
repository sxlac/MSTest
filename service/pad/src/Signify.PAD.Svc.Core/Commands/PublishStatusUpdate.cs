using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.PAD.Svc.Core.Events.Status;
using Signify.PAD.Svc.Core.Models;

namespace Signify.PAD.Svc.Core.Commands;

[Obsolete("To be removed in ANC-3978; please use the new PublishStatusUpdate command")]
[ExcludeFromCodeCoverage]
public class PublishStatusUpdateOld : IRequest
{
    public Guid EventId { get; }
    
    /// <summary>
    /// Status message to publish
    /// </summary>
    public PadStatusCode Status { get; }
    
    public PublishStatusUpdateOld(Guid eventId, PadStatusCode status)
    {
        EventId = eventId;
        Status = status;
    }
}

[ExcludeFromCodeCoverage]
public class PublishStatusUpdateHandlerOld : IRequestHandler<PublishStatusUpdateOld>
{
    private readonly ILogger<PublishStatusUpdateHandlerOld> _logger;
    private readonly IMessageProducer _messageProducer;

    public PublishStatusUpdateHandlerOld(ILogger<PublishStatusUpdateHandlerOld> logger, IMessageProducer messageProducer)
    {
        _logger = logger;
        _messageProducer = messageProducer;
    }

    public async Task Handle(PublishStatusUpdateOld request, CancellationToken cancellationToken)
    {
        await _messageProducer.Produce(request.Status.EvaluationId.ToString(), request.Status, cancellationToken);

        _logger.LogInformation("Published EventType={EventType} status update, for EvaluationId={EvaluationId} with EventId={EventId}",
            request.Status.GetType().Name, request.Status.EvaluationId, request.EventId);
    }
}

/// <summary>
/// Command to publish a status update event
/// </summary>
public class PublishStatusUpdate : IRequest
{
    public Guid EventId { get; }

    /// <summary>
    /// Status message to publish
    /// </summary>
    public BaseStatusMessage Status { get; }

    public PublishStatusUpdate(Guid eventId, BaseStatusMessage status)
    {
        EventId = eventId;
        Status = status;
    }
}

public class PublishStatusUpdateHandler : IRequestHandler<PublishStatusUpdate>
{
    private readonly ILogger _logger;
    private readonly IMessageProducer _messageProducer;

    public PublishStatusUpdateHandler(ILogger<PublishStatusUpdateHandler> logger,
        IMessageProducer messageProducer)
    {
        _logger = logger;
        _messageProducer = messageProducer;
    }

    public async Task Handle(PublishStatusUpdate request, CancellationToken cancellationToken)
    {
        await _messageProducer.Produce(request.Status.EvaluationId.ToString(), request.Status, cancellationToken);

        _logger.LogInformation("Enqueued EventType={EventType} status update for publishing to Kafka, for EvaluationId={EvaluationId} with EventId={EventId}",
            request.Status.GetType().Name, request.Status.EvaluationId, request.EventId);
    }
}