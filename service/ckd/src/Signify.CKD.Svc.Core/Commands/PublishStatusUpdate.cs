using MediatR;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.CKD.Svc.Core.Messages.Status;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.CKD.Svc.Core.Commands
{
    /// <summary>
    /// Command to publish a status update event
    /// </summary>
    public class PublishStatusUpdate : IRequest<Unit>
    {
        /// <summary>
        /// Status message to publish
        /// </summary>
        public BaseStatusMessage Status { get; }

        public PublishStatusUpdate(BaseStatusMessage status)
        {
            Status = status;
        }
    }

    public class PublishStatusUpdateHandler : IRequestHandler<PublishStatusUpdate, Unit>
    {
        private readonly ILogger _logger;
        private readonly IMessageProducer _messageProducer;

        public PublishStatusUpdateHandler(ILogger<PublishStatusUpdateHandler> logger, IMessageProducer messageProducer)
        {
            _logger = logger;
            _messageProducer = messageProducer;
        }

        public async Task<Unit> Handle(PublishStatusUpdate request, CancellationToken cancellationToken)
        {
            await _messageProducer
                .Produce(request.Status.EvaluationId.ToString(), request.Status, cancellationToken);

            _logger.LogInformation("Published EventType={EventType} status update for EvaluationId={EvaluationId}",
                request.Status.GetType().Name, request.Status.EvaluationId);

            return Unit.Value;
        }
    }
}
