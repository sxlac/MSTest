using System.Diagnostics.CodeAnalysis;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.PAD.Svc.Core.Events;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.PAD.Svc.Core.Commands;

[ExcludeFromCodeCoverage]
public class PublishResults : IRequest<bool>
{
    public ResultsReceived Event { get; }

    public int EvaluationId => Event.EvaluationId;

    public PublishResults(ResultsReceived @event)
    {
        Event = @event;
    }
}

public class PublishResultsHandler : IRequestHandler<PublishResults, bool>
{
    private readonly ILogger<PublishResultsHandler> _log;
    private readonly IMessageProducer _messageProducer;

    public PublishResultsHandler(ILogger<PublishResultsHandler> log, IMessageProducer messageProducer)
    {
        _log = log;
        _messageProducer = messageProducer;
    }

    public async Task<bool> Handle(PublishResults request, CancellationToken cancellationToken)
    {
        _log.LogInformation("Received request to publish results for EvaluationId={EvaluationId}", request.EvaluationId);

        await _messageProducer.Produce(request.EvaluationId.ToString(), request.Event, cancellationToken).ConfigureAwait(false);

        _log.LogInformation("Published results for EvaluationId={EvaluationId}", request.EvaluationId);

        return true;
    }
}