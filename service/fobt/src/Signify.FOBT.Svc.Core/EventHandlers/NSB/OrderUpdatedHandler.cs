using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.FOBT.Svc.Core.ApiClient.Requests;
using Signify.FOBT.Svc.Core.Commands;
using Signify.FOBT.Svc.Core.Data;
using Signify.FOBT.Svc.Core.Data.Entities;
using Signify.FOBT.Svc.Core.Events;
using Signify.FOBT.Svc.Core.Exceptions;
using Signify.FOBT.Svc.Core.Queries;
using System;
using System.Threading.Tasks;

using Fobt = Signify.FOBT.Svc.Core.Data.Entities.FOBT;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers;

public class OrderUpdatedHandler : IHandleMessages<BarcodeUpdate>
{
    private readonly ILogger _logger;
    private readonly ITransactionSupplier _transactionSupplier;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public OrderUpdatedHandler(ILogger<OrderUpdatedHandler> logger,
        ITransactionSupplier transactionSupplier,
        IMediator mediator,
        IMapper mapper)
    {
        _logger = logger;
        _transactionSupplier = transactionSupplier;
        _mediator = mediator;
        _mapper = mapper;
    }

    [Transaction]
    public async Task Handle(BarcodeUpdate message, IMessageHandlerContext context)
    {
        var fobt = await GetCorrespondingFobt(message);

        if (fobt.Barcode == message.Barcode && message.OrderCorrelationId == fobt.OrderCorrelationId)
        {                
            _logger.LogInformation("Nothing to do, neither Barcode {Barcode} nor OrderCorrelationId {OrderCorrelationId} have changed for EvaluationId {EvaluationId}",
                message.Barcode, message.OrderCorrelationId, message.EvaluationId);
            return;
        }

        using var transaction = _transactionSupplier.BeginTransaction();

        fobt = await UpdateFobtAndInsertBarcodeHistory(fobt, message);

        //3. Insert FOBT status
        await InsertStatus(fobt, FOBTStatusCode.OrderUpdated);

        // ANC-3032: We are suspending updates to the inventory service pending removal of WASP.
        // await UpdateInventory(fobt, context);

        await transaction.CommitAsync(context.CancellationToken);
    }

    private async Task<Fobt> GetCorrespondingFobt(BarcodeUpdate message)
    {
        var fobt = await _mediator.Send(new GetFOBT { EvaluationId = message.EvaluationId!.Value });
        if (fobt != null)
            return fobt;

        _logger.LogWarning("Unable to find an evaluation in db with EvaluationId {EvaluationId}", message.EvaluationId);
        throw new UnableToFindFobtException(message.EvaluationId.Value);
    }

    private async Task<Fobt> UpdateFobtAndInsertBarcodeHistory(Fobt fobt, BarcodeUpdate message)
    {
        var oldBarcode = fobt.Barcode;
        var oldOrderCorrelationId = fobt.OrderCorrelationId;
        fobt.Barcode = message.Barcode;
        fobt.OrderCorrelationId = message.OrderCorrelationId;
        var createOrUpdateFobt = _mapper.Map<CreateOrUpdateFOBT>(fobt);
        var updatedFobt = await _mediator.Send(createOrUpdateFobt);

        //2. Insert new record in BarcodeHistory table
        var updateBarcodeHistory = new CreateBarcodeHistory
        {
            FOBTId = fobt.FOBTId,
            Barcode = oldBarcode,
            OrderCorrelationId = oldOrderCorrelationId
        };

        await _mediator.Send(updateBarcodeHistory);

        if (!string.IsNullOrWhiteSpace(oldBarcode))
            return updatedFobt;

        // If we get here, this is the resolution of a "No Order Hold" (ie lab vendor received a sample that they
        // do not have an order for, so the sample is placed on hold, and not tested).
        //
        // This is different than any other process manager we have. There is a use-case where an evaluation may
        // be scheduled to include FOBT, but the provider may not have any kits on hand at the time they're in
        // the member's home during the IHE. In this case, the member will be sent a kit after the IHE (the same
        // way a member can be sent a new kit if the original kit has some exception, such as it's expired). In
        // this case, NotPerformed will be the status in db, because that is based on the "Colorectal Cancer
        // screening kit left today?" question was answered as "No" by the provider. But, since a kit was mailed
        // to the member after the IHE, we need to later associate the results to the evaluation.
        //
        // The `UpdateFobt` method called above handles associating this kit (specifically, the kit barcode) to
        // the evaluation.

        _logger.LogInformation("No Order Hold resolved for EvaluationId {EvaluationId}, with OrderCorrelationId {OrderCorrelationId} and Barcode {Barcode}",
            message.EvaluationId, message.OrderCorrelationId, message.Barcode);

        // This is different than any other process manager we have. Yes, this evaluation not only will have
        // NotPerformed status, but now also Performed status in db. This is correct because we now know a kit
        // was left with the member after the IHE, and the lab vendor received the sample. Another reason to
        // save Performed status here is to ensure when the pdf is delivered to the client, we bill for FOBT.
        await InsertStatus(fobt, FOBTStatusCode.FOBTPerformed);
        await InsertStatus(fobt, FOBTStatusCode.LabOrderCreated);

        return updatedFobt;
    }

    private Task InsertStatus(Fobt fobt, FOBTStatusCode statusCode)
    {
        return _mediator.Send(new CreateFOBTStatus
        {
            FOBT = fobt,
            StatusCode = statusCode
        });
    }

    private async Task UpdateInventory(Fobt fobt, IPipelineContext context)
    {
        var updateInventory = _mapper.Map<UpdateInventoryRequest>(fobt);
        updateInventory.RequestId = updateInventory.CorrelationId = Guid.NewGuid();

        await context.Send(updateInventory);

        _logger.LogInformation("Event enqueued to update inventory, with CorrelationId {CorrelationId}, from EvaluationId {EvaluationId}",
            updateInventory.CorrelationId, updateInventory.EvaluationId);
    }
}