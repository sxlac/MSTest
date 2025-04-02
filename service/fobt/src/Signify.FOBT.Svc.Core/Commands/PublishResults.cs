using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.FOBT.Svc.Core.Data.Entities;
using Signify.FOBT.Svc.Core.Events;
using System.Threading;
using System.Threading.Tasks;

using Fobt = Signify.FOBT.Svc.Core.Data.Entities.FOBT;

namespace Signify.FOBT.Svc.Core.Commands;

public class PublishResults : IRequest
{
	public Fobt Fobt { get; }
	public LabResults Results { get; }
	public bool IsBillable { get; }

	public PublishResults(Fobt fobt, LabResults results, bool isBillable)
	{
		Fobt = fobt;
		Results = results;
		IsBillable = isBillable;
	}
}

public class PublishResultsHandler : IRequestHandler<PublishResults>
{
	private readonly ILogger _logger;
	private readonly IMessageProducer _messageProducer;
	private readonly IMapper _mapper;

	public PublishResultsHandler(ILogger<PublishResultsHandler> logger,
		IMessageProducer messageProducer,
		IMapper mapper)
	{
		_logger = logger;
		_messageProducer = messageProducer;
		_mapper = mapper;
	}

	public async Task Handle(PublishResults request, CancellationToken cancellationToken)
	{
		var publishResultsEvent = _mapper.Map<Results>(request.Results);
		_mapper.Map(request.Fobt, publishResultsEvent);
		publishResultsEvent.IsBillable = request.IsBillable;

		await _messageProducer.Produce(request.Fobt.EvaluationId.ToString(), publishResultsEvent, cancellationToken)
			.ConfigureAwait(false);

		_logger.LogInformation("Published results for EvaluationId {EvaluationId}", request.Fobt.EvaluationId);
	}
}