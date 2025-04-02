using MediatR;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.Spirometry.Core.Events.Status;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.Spirometry.Core.Commands
{
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

        public PublishStatusUpdateHandler(ILogger<PublishStatusUpdateHandler> logger, IMessageProducer messageProducer)
        {
            _logger = logger;
            _messageProducer = messageProducer;
        }

        public async Task Handle(PublishStatusUpdate request, CancellationToken cancellationToken)
        {
            await _messageProducer.Produce(request.Status.EvaluationId.ToString(), request.Status, cancellationToken);

            _logger.LogInformation("Published EventType={EventType} status update, for EvaluationId={EvaluationId} with EventId={EventId}",
                request.Status.GetType().Name, request.Status.EvaluationId, request.EventId);
        }
    }
}
