using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Events.Akka;

namespace Signify.uACR.Core.Commands;

public class PublishResults(Exam exam, LabResult labResult, bool isBillable) : IRequest<Unit>
{
    public Exam Exam { get; } = exam;
    public LabResult LabResult { get; } = labResult;

    public bool IsBillable { get; } = isBillable;
}

public class PublishResultsHandler(
    ILogger<PublishResultsHandler> logger,
    IMessageProducer messageProducer,
    IMapper mapper)
    : IRequestHandler<PublishResults, Unit>
{
    public async Task<Unit> Handle(PublishResults request, CancellationToken cancellationToken)
    {
        var publishResultsEvent = mapper.Map<ResultsReceived>(request.LabResult);
        mapper.Map(request.Exam, publishResultsEvent);
        publishResultsEvent.IsBillable = request.IsBillable;
        await messageProducer.Produce(request.Exam.EvaluationId.ToString(), publishResultsEvent, cancellationToken)
            .ConfigureAwait(false);

        logger.LogInformation("Published results for EvaluationId {EvaluationId}", request.Exam.EvaluationId);

        return Unit.Value;
    }
}