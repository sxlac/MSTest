using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.CKD.Messages.Events;
using Signify.CKD.Svc.Core.Messages.Status;
using Signify.CKD.Svc.Core.Commands;
using Signify.CKD.Svc.Core.Constants;
using Signify.CKD.Svc.Core.Infrastructure.Observability;

namespace Signify.CKD.Svc.Core.EventHandlers
{
    public class CKDPerformedHandler : IHandleMessages<CKDPerformed>
	{
		private readonly IMapper _mapper;
		private readonly IMediator _mediator;
		private readonly IObservabilityService _observabilityService;
		private readonly ILogger<CKDPerformedHandler> _logger;

		public CKDPerformedHandler(ILogger<CKDPerformedHandler> logger, 
			IMapper mapper, 
			IMediator mediator, 
			IObservabilityService observabilityService)
		{
			_logger = logger;
			_mapper = mapper;
			_mediator = mediator;
			_observabilityService = observabilityService;
		}

		[Transaction]
		public async Task Handle(CKDPerformed message, IMessageHandlerContext context)
		{
			_logger.LogInformation("Received performed event for EvaluationId={EvaluationId}", message.EvaluationId);

			//Publish to Kafka
			var kafkaPerformedMessage = _mapper.Map<Performed>(message);
			await _mediator.Send(new PublishStatusUpdate(kafkaPerformedMessage));

			#if false // Automated inventory-related processing with WASP is disabled, at least for now
			var updateInventory = _mapper.Map<UpdateInventory>(message);
			Send InventoryUpdate command via NServiceBus
			await context.SendLocal(updateInventory);
			#endif

			_logger.LogInformation("Finished process performed event for EvaluationId={EvaluationId}", message.EvaluationId);
			
			_observabilityService.AddEvent(Observability.Evaluation.EvaluationPerformedEvent, new Dictionary<string, object>()
			{
				{Observability.EventParams.EvaluationId, message.EvaluationId},
				{Observability.EventParams.CreatedDateTime, message.CreatedDateTime.ToUnixTimeSeconds()}
			});
		}
	}
}
