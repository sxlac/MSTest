using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.A1C.Core.Events;
using Signify.A1C.Messages.Events;
using Signify.A1C.Svc.Core.ApiClient;
using Signify.A1C.Svc.Core.ApiClient.Requests;
using Signify.A1C.Svc.Core.Commands;
using Signify.A1C.Svc.Core.Data;
using Signify.A1C.Svc.Core.Data.Entities;
using Signify.AkkaStreams.Kafka;

namespace Signify.A1C.Svc.Core.EventHandlers
{
    /// <summary>
    ///This handles BarCodeUpdated event.
    /// </summary>
    public class BarCodeUpdateHandler : IHandleEvent<BarcodeUpdate>
    {
        private readonly ILogger<BarCodeUpdateHandler> _logger;
        private const string ProductCode = "HBA1C";
        private readonly IMediator _mediator;
        private readonly A1CDataContext _dataContext;
        private readonly IMapper _mapper;
        private readonly IEndpointInstance _endpoint;
        private readonly IEvaluationApi _evalApi;

        public BarCodeUpdateHandler(ILogger<BarCodeUpdateHandler> logger, IMediator mediator, IMapper mapper, A1CDataContext dataContext, IEndpointInstance endpoint, IEvaluationApi evalApi)
        {
            _logger = logger;
            _mediator = mediator;
            _mapper = mapper;
            _dataContext = dataContext;
            _endpoint = endpoint;
            _evalApi = evalApi;
        }

        //look for details in https://app.lucidchart.com/documents/edit/e3a5aacc-d0aa-49f4-9fda-dac79a6ae5f8/PGqqEGkfbdPo#?folder_id=home&browser=icon

        [Transaction]
        public async Task Handle(BarcodeUpdate @event, CancellationToken cancellationToken)
        {
            _logger.LogDebug($"Start Handle BarCodeUpdatedEvent, EvaluationId: {@event.EvaluationId}");
            //Filter product code
            var isA1CProduct = string.Equals(@event.ProductCode, ProductCode, StringComparison.OrdinalIgnoreCase);
            if (!isA1CProduct)
            {
                _logger.LogDebug($"Task Completed. Evaluation Ignored for non-A1C product, EvaluationID: {@event.EvaluationId}");
                return;
            }
            //Check for evaluation id
            if (!@event.EvaluationId.HasValue || @event.EvaluationId <= 0)
            {
                _logger.LogDebug($"Task Completed. barcode_update event ignored since evaluation not found, EvaluationID:{@event.EvaluationId}");
                return;
            }

            //Query database to check if A1C exists.
            var a1C = await _mediator.Send(new Queries.GetA1C { EvaluationId = Convert.ToInt32(@event.EvaluationId) }, cancellationToken);

            if (!await SetA1CAsync(@event, a1C)) return;

            await using (var transaction = await _dataContext.Database.BeginTransactionAsync(cancellationToken))
            {
                var oldBarCode = a1C.Barcode;
                a1C.Barcode = @event.Barcode;
                a1C.OrderCorrelationId = @event.OrderCorrelationId ?? a1C.OrderCorrelationId;

                //1. Update Barcode and OrderCorrelationId in A1C
                var createOrUpdateA1C = _mapper.Map<CreateOrUpdateA1C>(a1C);
                await _mediator.Send(createOrUpdateA1C, cancellationToken);

                //2. Insert into Barcode History
                var updateBarcodeHistory = _mapper.Map<UpdateBarcodeHistory>(@event);
                updateBarcodeHistory.A1CId = a1C.A1CId;
                updateBarcodeHistory.Barcode = oldBarCode;
                await _mediator.Send(updateBarcodeHistory, cancellationToken);

                // Check if A1CEvalResponse is empty that means A1CNotPerformed and create status A1CPerformed and LabOrderCreated while doing match from UI.
                var checka1cEvalResponse = await _mediator.Send(new Queries.CheckA1CEval { EvaluationId = (int)@event.EvaluationId }, cancellationToken);
                if (string.IsNullOrWhiteSpace(checka1cEvalResponse))
                {
                    _logger.LogInformation($"EvaluationID: {@event.EvaluationId} has A1CNotPerformed status, Updating status A1CPerformed and LabOrderCreated while doing match.");
                    await _mediator.Send(new CreateA1CStatus()
                    { A1CId = a1C.A1CId, StatusCode = A1CStatusCode.A1CPerformed }, cancellationToken);
                    await _mediator.Send(new CreateA1CStatus()
                    { A1CId = a1C.A1CId, StatusCode = A1CStatusCode.LabOrderCreated }, cancellationToken);
                }

                //3. Insert into A1C Status
                await _mediator.Send(new CreateA1CStatus()
                { A1CId = a1C.A1CId, StatusCode = A1CStatusCode.BarcodeUpdated }, cancellationToken);

                var updateInventory = _mapper.Map<UpdateInventoryRequest>(a1C);
                updateInventory.RequestId = updateInventory.CorrelationId = Guid.NewGuid();

                //Send InventoryUpdate command via NServiceBus
                await _endpoint.Send(updateInventory);
                await transaction.CommitAsync(cancellationToken);
            }

            _logger.LogDebug($"End Handle BarcodeUpdatedEvent, EvaluationId: {@event.EvaluationId}");
        }



        private async Task<bool> SetA1CAsync(BarcodeUpdate @event, Data.Entities.A1C a1C)
        {
            if (a1C == null)
            {
                _logger.LogDebug($"Evaluation not exists, EvaluationID: {@event.EvaluationId}, inserting the new evaluation");
                var evalVerRs = await _evalApi.GetEvaluationVersion(@event.EvaluationId.Value);
                var newA1CEval = _mapper.Map<A1CEvaluationReceived>(evalVerRs.Evaluation);
                newA1CEval = _mapper.Map(@event, newA1CEval);
                //Raise A1C Evaluation Received NService bus event
                await _endpoint.Publish(newA1CEval);
                return false;
            }
            if (a1C?.Barcode != @event.Barcode) return true;
            _logger.LogDebug($"Evaluation Barcode is same for EvaluationID: {@event.EvaluationId}");
            return false;

        }
    }
}