using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.FOBT.Svc.Core.ApiClient;
using Signify.FOBT.Svc.Core.ApiClient.Requests;
using Signify.FOBT.Svc.Core.Commands;
using Signify.FOBT.Svc.Core.Data.Entities;
using Signify.FOBT.Svc.Core.Exceptions;
using Signify.FOBT.Svc.Core.Queries;
using Signify.FOBT.Svc.Core.Sagas.Models;

namespace Signify.FOBT.Svc.Core.Sagas;

[ExcludeFromCodeCoverage]
public class UpdateInventorySaga : Saga<UpdateInventorySagaData>,
    IAmStartedByMessages<UpdateInventoryRequest>,
    IHandleMessages<InvUpdateReceived>
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
        mapper.MapSaga(saga => saga.CorrelationId)
            .ToMessage<UpdateInventoryRequest>(message => message.CorrelationId)
            .ToMessage<InvUpdateReceived>(message => message.RequestId);
    }

    [Transaction]
    public async Task Handle(UpdateInventoryRequest message, IMessageHandlerContext context)
    {
        var fobt = _mapper.Map<Data.Entities.FOBT>(message);

        //Call inventory Api to update inventory status

        var updateInventoryResponse = await _inventoryApi.Inventory(message);
        if (updateInventoryResponse != null && updateInventoryResponse.Success)
        {
            //Set Saga Data
            Data.CorrelationId = updateInventoryResponse.RequestId;
            Data.FOBTId = message.FOBTId;

            //log FOBT status "InventoryUpdateRequested"
            await _mediator.Send(new CreateFOBTStatus
            {
                FOBT = fobt,
                StatusCode = FOBTStatusCode.InventoryUpdateRequested
            }, context.CancellationToken);
        }
        else
        {
            _logger.LogInformation("Error calling inventory api, EvaluationId:{EvaluationId}", message.EvaluationId);
            //Throw exception to hit retry process
            throw new InventoryException(message.EvaluationId, message.FOBTId, context.MessageId, "Unable to decrement inventory");
        }
    }

    [Transaction]
    public async Task Handle(InvUpdateReceived message, IMessageHandlerContext context)
    {
        _logger.LogInformation(
            "Creating inventory updated status: BarCode:{SerialNumber}, ResultMessage: {ErrorMessage}", message.SerialNumber, message.Result.ErrorMessage);


        var queryFobtRs = await _mediator.Send(new QueryFOBTWithId { FOBTId = Data.FOBTId }, context.CancellationToken);

        var evtStatusCode = message.Result.IsSuccess
            ? FOBTStatusCode.InventoryUpdateSuccess
            : FOBTStatusCode.InventoryUpdateFail;

        //log FOBT status InventoryUpdateSuccess or InventoryUpdateFail
        await _mediator.Send(new CreateFOBTStatus { FOBT = queryFobtRs.FOBT, StatusCode = evtStatusCode }, context.CancellationToken);


        _logger.LogDebug(
            "End Handle InventoryUpdated, ItemNumber: {ItemNumber}, Barcode: {SerialNumber}", message.ItemNumber, message.SerialNumber);

        //mark saga complete, which removes entry from db.
        MarkAsComplete();
    }
}