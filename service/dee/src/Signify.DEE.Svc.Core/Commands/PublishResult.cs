using System.Diagnostics.CodeAnalysis;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using System.Threading;
using System.Threading.Tasks;
using Result = Signify.DEE.Messages.Result;

namespace Signify.DEE.Svc.Core.Commands;

[ExcludeFromCodeCoverage]
public class PublishResult(Result results) : IRequest<Unit>
{
    public Result Results { get; } = results;
}

public class PublishResultHandler(ILogger<PublishResultHandler> logger, IMessageProducer messageProducer)
    : IRequestHandler<PublishResult, Unit>
{
    public async Task<Unit> Handle(PublishResult request, CancellationToken cancellationToken)
    {
        await messageProducer.Produce(request.Results.EvaluationId.ToString(), request.Results, cancellationToken);

        logger.LogInformation("Published EventType={EventType} for EvaluationId={EvaluationId}",
            nameof(Result), request.Results.EvaluationId);

        return Unit.Value;
    }
}