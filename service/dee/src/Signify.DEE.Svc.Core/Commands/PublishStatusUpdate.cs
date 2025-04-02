using System.Diagnostics.CodeAnalysis;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.DEE.Messages.Status;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.Commands;

/// <summary>
/// Command to publish a status update event
/// </summary>
[ExcludeFromCodeCoverage]
public class PublishStatusUpdate(BaseStatusMessage status) : IRequest<Unit>
{
    /// <summary>
    /// Status message to publish
    /// </summary>
    public BaseStatusMessage Status { get; } = status;
}

public class PublishStatusUpdateHandler(ILogger<PublishStatusUpdateHandler> logger, IMessageProducer messageProducer)
    : IRequestHandler<PublishStatusUpdate, Unit>
{
    public async Task<Unit> Handle(PublishStatusUpdate request, CancellationToken cancellationToken)
    {
        await messageProducer.Produce(request.Status.EvaluationId.ToString(), request.Status, cancellationToken);

        logger.LogInformation("Published EventType={EventType} status update for EvaluationId={EvaluationId}",
            request.Status.GetType().Name, request.Status.EvaluationId);

        return Unit.Value;
    }
}