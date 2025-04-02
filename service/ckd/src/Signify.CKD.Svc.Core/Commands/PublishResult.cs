using MediatR;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using System.Threading;
using System.Threading.Tasks;
using Result = Signify.CKD.Svc.Core.Messages.Result;

namespace Signify.CKD.Svc.Core.Commands
{
    public class PublishResult : IRequest<Unit>
    {
        public Result Results { get; }

        public PublishResult(Result result)
        {
            Results = result;
        }
    }

    public class PublishResultHandler : IRequestHandler<PublishResult, Unit>
    {
        private readonly ILogger _logger;
        private readonly IMessageProducer _messageProducer;

        public PublishResultHandler(ILogger<PublishResultHandler> logger, IMessageProducer messageProducer)
        {
            _logger = logger;
            _messageProducer = messageProducer;
        }

        public async Task<Unit> Handle(PublishResult request, CancellationToken cancellationToken)
        {
            await _messageProducer.Produce(request.Results.EvaluationId.ToString(), request.Results, cancellationToken);

            _logger.LogInformation("Published EventType={EventType} for EvaluationId={EvaluationId}",
                nameof(Result), request.Results.EvaluationId);

            return Unit.Value;
        }
    }
}