using System;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.CKD.Sagas;
using Signify.CKD.Svc.Core.ApiClient;
using Signify.CKD.Svc.Core.ApiClient.Requests;
using Signify.CKD.Svc.Core.Commands;
using Signify.CKD.Svc.Core.Data;
using Signify.CKD.Svc.Core.Data.Entities;
using Signify.CKD.Svc.Core.Sagas.Commands;
using Signify.CKD.Svc.Core.Sagas.Models;

namespace Signify.CKD.Svc.Core.Sagas
{
	/// <summary>
	/// Updates inventory by calling the Inventory API and records inventory update status
	/// </summary>
    public class UpdateInventorySaga : Saga<UpdateInventorySagaData>,
		IAmStartedByMessages<UpdateInventory>,
		IHandleMessages<InventoryUpdateReceived>
	{

		private readonly ILogger<UpdateInventorySaga> _logger;
		private readonly IMapper _mapper;
		private readonly IMediator _mediator;
		private readonly IInventoryApi _inventoryApi;
		private readonly CKDDataContext _dataContext;

		public UpdateInventorySaga(ILogger<UpdateInventorySaga> logger,
			IMapper mapper,
			IMediator mediator,
			IInventoryApi inventoryApi,
			CKDDataContext dataContext)
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
			var ckd = _mapper.Map<Data.Entities.CKD>(message);

			//Call inventory Api to update inventory status
			var updateInventoryRequest = _mapper.Map<UpdateInventoryRequest>(message);

			var updateInventoryResponse = await _inventoryApi.Inventory(updateInventoryRequest);
			if (updateInventoryResponse != null && updateInventoryResponse.Success)
			{
				//Set Saga Data
				Data.CorrelationId = updateInventoryResponse.RequestId;
				Data.CKDId = message.CKDId;

				//log CKD status "InventoryUpdateRequested"
				await _mediator.Send(new CreateCKDStatus
				{
					CKDId = ckd.CKDId,
					StatusCodeId = CKDStatusCode.InventoryUpdateRequested.CKDStatusCodeId
				});
			}
			else
			{
				_logger.LogInformation($"Error calling inventory api, EvaluationId:{message.EvaluationId}:");
				//Throw exception to hit retry process
				throw new ApplicationException($"Unable to decrement inventory for CKDId:{message.CKDId}, MessageId: {context.MessageId}");
			}
		}

		[Transaction]
		public async Task Handle(InventoryUpdateReceived message, IMessageHandlerContext context)
		{
			var evtStatusCode = message.Result.IsSuccess
				? CKDStatusCode.InventoryUpdateSuccess
				: CKDStatusCode.InventoryUpdateFail;

			var ckd = await _dataContext.CKD.SingleAsync(s => s.CKDId == Data.CKDId);

			//log CKD status InventoryUpdateSuccess or InventoryUpdateFail
			await _mediator.Send(new CreateCKDStatus { CKDId = ckd.CKDId, StatusCodeId = evtStatusCode.CKDStatusCodeId });

			//mark saga complete, which removes entry from db
			MarkAsComplete();
		}
	}
}
