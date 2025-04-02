using System;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.HBA1CPOC.Sagas;
using Signify.HBA1CPOC.Svc.Core.ApiClient;
using Signify.HBA1CPOC.Svc.Core.ApiClient.Requests;
using Signify.HBA1CPOC.Svc.Core.Commands;
using Signify.HBA1CPOC.Svc.Core.Data;
using Signify.HBA1CPOC.Svc.Core.Data.Entities;
using Signify.HBA1CPOC.Svc.Core.Sagas.Commands;
using Signify.HBA1CPOC.Svc.Core.Sagas.Models;

namespace Signify.HBA1CPOC.Svc.Core.Sagas
{
	/// <summary>
	/// This saga updates inventory by calling inventory api and records inventory update results status
	/// </summary>
	public class UpdateInventorySaga : Saga<UpdateInventorySagaData>,
		IAmStartedByMessages<UpdateInventory>,
		IHandleMessages<InventoryUpdateReceived>
	{
		private readonly ILogger _logger;
		private readonly IMapper _mapper;
		private readonly IMediator _mediator;
		private readonly IInventoryApi _inventoryApi;
		private readonly Hba1CpocDataContext _dataContext;

		public UpdateInventorySaga(ILogger<UpdateInventorySaga> logger,
			IMapper mapper,
			IMediator mediator,
			IInventoryApi inventoryApi,
			Hba1CpocDataContext dataContext)
		{
			_logger = logger;
			_mapper = mapper;
			_mediator = mediator;
			_inventoryApi = inventoryApi;
			_dataContext = dataContext;
		}

		protected override void ConfigureHowToFindSaga(SagaPropertyMapper<UpdateInventorySagaData> mapper)
		{
			mapper.MapSaga(saga => saga.CorrelationId)
				.ToMessage<UpdateInventory>(message => message.CorrelationId)
				.ToMessage<InventoryUpdateReceived>(message => message.RequestId);
		}

		[Transaction]
		public async Task Handle(UpdateInventory message, IMessageHandlerContext context)
		{
			var hbA1cpoc = _mapper.Map<Data.Entities.HBA1CPOC>(message);

			//Call inventory Api to update inventory status
			var updateInventoryRequest = _mapper.Map<UpdateInventoryRequest>(message);

			var updateInventoryResponse = await _inventoryApi.Inventory(updateInventoryRequest);
			if (updateInventoryResponse != null && updateInventoryResponse.Success)
			{
				//Set Saga Data
				Data.CorrelationId = updateInventoryResponse.RequestId;
				Data.HBA1CPOCId = message.HBA1CPOCId;

				//log HBA1CPOC status "InventoryUpdateRequested"
				await _mediator.Send(new CreateHBA1CPOCStatus()
				{ 
					HBA1CPOCId = hbA1cpoc.HBA1CPOCId, 
					StatusCodeId = HBA1CPOCStatusCode.InventoryUpdateRequested.HBA1CPOCStatusCodeId 
				}, context.CancellationToken);
			}
			else
			{
				_logger.LogWarning("Failed to get a successful response when calling inventory api, for EvaluationId={EvaluationId}", message.EvaluationId);

				//Throw exception to hit retry process
				// If we later re-enable inventory updates, this should be updated to a custom exception; not bothering with this now
				throw new InvalidOperationException($"Unable to decrement inventory for HBA1CPOCId:{message.HBA1CPOCId}, MessageId: {context.MessageId}");
			}
		}

		[Transaction]
		public async Task Handle(InventoryUpdateReceived message, IMessageHandlerContext context)
		{
			var evtStatusCode = message.Result.IsSuccess
				? HBA1CPOCStatusCode.InventoryUpdateSuccess
				: HBA1CPOCStatusCode.InventoryUpdateFail;

			var hbA1cpoc = await _dataContext.HBA1CPOC.SingleAsync(s => s.HBA1CPOCId == Data.HBA1CPOCId, context.CancellationToken);

			//log HBA1CPOC status InventoryUpdateSuccess or InventoryUpdateFail
			await _mediator.Send(new CreateHBA1CPOCStatus() { HBA1CPOCId = hbA1cpoc.HBA1CPOCId, StatusCodeId = evtStatusCode.HBA1CPOCStatusCodeId }, context.CancellationToken);

			//mark saga complete, which removes entry from db.
			MarkAsComplete();
		}
	}
}
