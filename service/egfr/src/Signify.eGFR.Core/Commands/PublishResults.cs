using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using Signify.AkkaStreams.Kafka;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Events.Akka;

namespace Signify.eGFR.Core.Commands;

public class PublishResults(Exam exam, ResultsReceived resultsReceived, bool isBillable)
	: IRequest<Unit>
{
	public Exam Exam { get; } = exam;
	public ResultsReceived ResultsReceived { get; } = resultsReceived;
	public bool IsBillable { get; } = isBillable;
}

public class PublishResultsHandler(
	ILogger<PublishResultsHandler> logger,
	IMessageProducer messageProducer,
	IMapper mapper)
	: IRequestHandler<PublishResults, Unit>
{
	private readonly ILogger _logger = logger;

	public async Task<Unit> Handle(PublishResults request, CancellationToken cancellationToken)
	{
		mapper.Map(request.Exam, request.ResultsReceived);
		request.ResultsReceived.IsBillable = request.IsBillable;

		await messageProducer.Produce(request.Exam.EvaluationId.ToString(), request.ResultsReceived, cancellationToken)
			.ConfigureAwait(false);

		_logger.LogInformation("Published results for EvaluationId {EvaluationId}", request.Exam.EvaluationId);

		return Unit.Value;
	}
}