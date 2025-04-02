using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Signify.CKD.Messages.Events;
using Signify.CKD.Svc.Core.Events;
using System.Threading;
using System.Threading.Tasks;
using NewRelic.Api.Agent;
using Signify.AkkaStreams.Kafka;
using Signify.CKD.Svc.Core.Constants;
using Signify.CKD.Svc.Core.Filters;
using Signify.CKD.Svc.Core.Infrastructure.Observability;

namespace Signify.CKD.Svc.Core.EventHandlers;

/// <summary>
///This handles evaluation finalized event. It filters CKD products and raise CKD Received Event.
/// </summary>
public class EvaluationFinalizedHandler : IHandleEvent<EvaluationFinalizedEvent>
{
	private readonly ILogger _logger;
	private readonly IProductFilter _productFilter;
	private readonly IMessageSession _messageSession;
	private readonly IMapper _mapper;
	private readonly IObservabilityService _observabilityService;

	public EvaluationFinalizedHandler(ILogger<EvaluationFinalizedHandler> logger,
		IProductFilter productFilter,
		IMessageSession messageSession, 
		IMapper mapper, 
		IObservabilityService observabilityService)
	{
		_logger = logger;
		_productFilter = productFilter;
		_messageSession = messageSession;
		_mapper = mapper;
		_observabilityService = observabilityService;
	}

	[Transaction]
	public async Task Handle(EvaluationFinalizedEvent @event, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Received Finalized event with {Count} product codes: {ProductCodes}, for EvaluationId={EvaluationId} with EventId={EventId}",
			@event.Products.Count, string.Join(',', @event.Products.Select(p => p.ProductCode)), @event.EvaluationId, @event.Id);

		if (!_productFilter.ShouldProcess(@event.Products))
		{
			_logger.LogDebug("Event ignored, for EvaluationId={EvaluationId} with EventId={EventId}", @event.EvaluationId, @event.Id);
			return;
		}

		// Raise CKD Evaluation Received NService bus event
		var evalReceived = _mapper.Map<EvalReceived>(@event);

		await _messageSession.SendLocal(evalReceived);

		_logger.LogInformation("Event queued for processing, for EvaluationId={EvaluationId} with EventId={EventId}", @event.EvaluationId, @event.Id);
		
		_observabilityService.AddEvent(Observability.Evaluation.EvaluationFinalizedEvent, new Dictionary<string, object>()
		{
			{Observability.EventParams.EvaluationId, @event.EvaluationId},
			{Observability.EventParams.CreatedDateTime, @event.CreatedDateTime.ToUnixTimeSeconds()}
		});
	}
}