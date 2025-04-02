using System;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.A1C.Svc.Core.ApiClient;
using Signify.A1C.Svc.Core.ApiClient.Requests;
using Signify.A1C.Svc.Core.Commands;
using Signify.A1C.Svc.Core.Data.Entities;
using Signify.A1C.Svc.Core.Queries;
using Signify.A1C.Svc.Core.Sagas.Models;

namespace Signify.A1C.Svc.Core.Sagas
{
    public class UpdateInventorySaga : Saga<UpdateInventorySagaData>,
        IAmStartedByMessages<UpdateInventoryRequest>,
        IHandleMessages<InventoryUpdateReceived>
    {
        private readonly ILogger<UpdateInventorySaga> _logger;
        private readonly IMapper _mapper;
        private readonly IMediator _mediator;
        private readonly IInventoryApi _inventoryApi;

        /// <summary>
        /// This saga updates inventory by calling inventory api and records inventory update results status. 
        /// </summary>
        public UpdateInventorySaga(ILogger<UpdateInventorySaga> logger, IMapper mapper,
            IMediator mediator, IInventoryApi inventoryApi)
        {
            _logger = logger;
            _mapper = mapper;
            _mediator = mediator;
            _inventoryApi = inventoryApi;
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<UpdateInventorySagaData> mapper)
        {
            mapper.ConfigureMapping<UpdateInventoryRequest>(message => message.CorrelationId)
                .ToSaga(sagaData => sagaData.CorrelationId);
            mapper.ConfigureMapping<InventoryUpdateReceived>(message => message.RequestId)
                .ToSaga(sagaData => sagaData.CorrelationId);
        }

       [Transaction]
        public async Task Handle(UpdateInventoryRequest message, IMessageHandlerContext context)
        {
            var a1C = _mapper.Map<Data.Entities.A1C>(message);

            //Call inventory Api to update inventory status

            var updateInventoryResponse = await _inventoryApi.Inventory(message);
            if (updateInventoryResponse != null && updateInventoryResponse.Success)
            {
                //Set Saga Data
                Data.CorrelationId = updateInventoryResponse.RequestId;
                Data.HBA1CId = message.A1CId;

                //log HBA1C status "InventoryUpdateRequested"
                await _mediator.Send(new CreateA1CStatus
                {
                    A1CId = a1C.A1CId,
                    StatusCode = A1CStatusCode.InventoryUpdateRequested
                });
            }
            else
            {
                _logger.LogInformation($"Error calling inventory api, EvaluationId{message.EvaluationId}:");
                //Throw exception to hit retry process
                throw new ApplicationException(
                    $"Unable to decrement inventory for HbA1C:{message.A1CId}, MessageId: {context.MessageId}");
            }
        }

        [Transaction]
        public async Task Handle(InventoryUpdateReceived message, IMessageHandlerContext context)
        {
            _logger.LogInformation(
                $"Creating inventory updated status: BarCode:{message.SerialNumber}, ResultMessage: {message.Result.ErrorMessage}");

            var queryA1CRs = await _mediator.Send(new QueryA1CWithId { HBA1CId = Data.HBA1CId });

            var evtStatusCode = message.Result.IsSuccess
                ? A1CStatusCode.InventoryUpdateSuccess
                : A1CStatusCode.InventoryUpdateFail;

            //log A1C status InventoryUpdateSuccess or InventoryUpdateFail
            await _mediator.Send(new CreateA1CStatus {
                A1CId = queryA1CRs.A1C.A1CId,
                StatusCode = evtStatusCode
            });

            _logger.LogDebug(
                $"End Handle InventoryUpdated, ItemNumber: {message.ItemNumber}, Barcode: {message.SerialNumber}");

            //mark saga complete, which removes entry from db.
            MarkAsComplete();
        }
    }
}