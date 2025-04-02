using MediatR;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using System.Threading;
using System.Threading.Tasks;
using Signify.HBA1CPOC.Messages.Events.Akka;

namespace Signify.HBA1CPOC.Svc.Core.Commands
{
    /// <summary>
    /// Command to publish an event containing lab results
    /// </summary>
    public class PublishLabResults : IRequest<Unit>
    {
        public ResultsReceived Results { get; }

        public PublishLabResults(ResultsReceived results)
        {
            Results = results;
        }
    }

    public class PublishLabResultsHandler : IRequestHandler<PublishLabResults, Unit>
    {
        private readonly ILogger _logger;
        private readonly IMessageProducer _messageProducer;

        public PublishLabResultsHandler(ILogger<PublishLabResultsHandler> logger, IMessageProducer messageProducer)
        {
            _logger = logger;
            _messageProducer = messageProducer;
        }

        public async Task<Unit> Handle(PublishLabResults request, CancellationToken cancellationToken)
        {
            await _messageProducer.Produce(request.Results.EvaluationId.ToString(), request.Results, cancellationToken);

            _logger.LogInformation("Published EventType={EventType} status update for EvaluationId={EvaluationId}",
                nameof(ResultsReceived), request.Results.EvaluationId);

            return Unit.Value;
        }
    }
}