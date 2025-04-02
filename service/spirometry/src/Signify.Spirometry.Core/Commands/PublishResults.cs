using MediatR;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.Spirometry.Core.Events.Akka;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.Spirometry.Core.Commands
{
    public class PublishResults : IRequest
    {
        public Guid EventId { get; }

        public ResultsReceived Event { get; }

        public int EvaluationId => Event.EvaluationId;

        public PublishResults(ResultsReceived @event)
            : this(@event, Guid.Empty) { }

        public PublishResults(ResultsReceived @event, Guid eventId)
        {
            Event = @event;
            EventId = eventId;
        }
    }

    public class PublishResultsHandler : IRequestHandler<PublishResults>
    {
        private readonly ILogger _logger;
        private readonly IMessageProducer _messageProducer;

        public PublishResultsHandler(ILogger<PublishResultsHandler> logger, IMessageProducer messageProducer)
        {
            _logger = logger;
            _messageProducer = messageProducer;
        }

        public async Task Handle(PublishResults request, CancellationToken cancellationToken)
        {
            await _messageProducer.Produce(request.EvaluationId.ToString(), request.Event, cancellationToken);

            if (request.EventId != Guid.Empty)
                _logger.LogInformation("Enqueued publishing results to Kafka, for EvaluationId={EvaluationId}, EventId={EventId}", request.EvaluationId, request.EventId);
            else
                _logger.LogInformation("Enqueued publishing results to Kafka, for EvaluationId={EvaluationId}", request.EvaluationId);
        }
    }
}
