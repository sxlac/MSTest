using MediatR;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.PAD.Svc.Core.Models;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.PAD.Svc.Core.Commands;

[ExcludeFromCodeCoverage]
public class PublishAoeResult : IRequest<Unit>
{
    public AoeResult AoeResult { get; set; }

    public PublishAoeResult(AoeResult aoeResult)
    {
        AoeResult = aoeResult;
    }
}

public class PublishAoeResultHandler : IRequestHandler<PublishAoeResult, Unit>
{
    private readonly ILogger _logger;
    private readonly IMessageProducer _messageProducer;

    public PublishAoeResultHandler(ILogger<PublishAoeResultHandler> logger, IMessageProducer messageProducer)
    {
        _logger = logger;
        _messageProducer = messageProducer;
    }

    public async Task<Unit> Handle(PublishAoeResult request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Publishing AoE result for EvaluationId={EvaluationId}", request.AoeResult.EvaluationId);
        await _messageProducer.Produce(request.AoeResult.EvaluationId.ToString(), request.AoeResult, cancellationToken);
        _logger.LogInformation("Published AoE result for EvaluationId={EvaluationId}", request.AoeResult.EvaluationId);

        return Unit.Value;
    }
}
