using AutoMapper;
using Fobt = Signify.FOBT.Svc.Core.Data.Entities.FOBT;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using OrderHeld = Signify.FOBT.Svc.Core.Events.OrderHeld;
using OrderHeldStatus = Signify.FOBT.Svc.Core.Events.Status.OrderHeld;
using Signify.FOBT.Svc.Core.Commands;
using Signify.FOBT.Svc.Core.Data;
using Signify.FOBT.Svc.Core.Data.Entities;
using Signify.FOBT.Svc.Core.Exceptions;
using Signify.FOBT.Svc.Core.Queries;
using System.Linq;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers;

public class OrderHeldHandler : IHandleMessages<OrderHeld>
{
    private readonly ILogger _logger;
    private readonly ITransactionSupplier _transactionSupplier;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public OrderHeldHandler(ILogger<OrderHeldHandler> logger,
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
    public async Task Handle(OrderHeld message, IMessageHandlerContext context)
    {
        var fobt = await GetCorrespondingFobt(message);

        using var transaction = _transactionSupplier.BeginTransaction();

        await InsertStatus(fobt, FOBTStatusCode.OrderHeld);

        var orderHeldstatus = _mapper.Map<OrderHeldStatus>(fobt);

        _mapper.Map(message, orderHeldstatus);

        await _mediator.Send(new PublishStatusUpdate(orderHeldstatus), context.CancellationToken);

        await transaction.CommitAsync(context.CancellationToken);
    }

    private async Task<Fobt> GetCorrespondingFobt(OrderHeld message)
    {
        var fobt = await _mediator.Send(new GetFobtByBarcode { Barcode = message.Barcode });
        switch (fobt.Count)
        {
            case 0:
                return await GetFobtFromBarcodeHistory(message.Barcode);
            case 1:
                return fobt.First();
            default:
                _logger.LogWarning("Duplicate barcode found for Barcode {Barcode}", message.Barcode);
                throw new DuplicateBarcodeFoundException(message.Barcode);
        }
    }

    private Task<FOBTStatus> InsertStatus(Fobt fobt, FOBTStatusCode statusCode)
    {
        return _mediator.Send(new CreateFOBTStatus
        {
            FOBT = fobt,
            StatusCode = statusCode
        });
    }

    private async Task<Fobt> GetFobtFromBarcodeHistory(string barcode)
    {
        var barcodehistory = await _mediator.Send(new GetBarcodeHistory { Barcode = barcode });

        switch (barcodehistory.Count)
        {
            case 0:
                _logger.LogWarning("Unable to find an evaluation in db with Barcode {Barcode}", barcode);
                throw new UnableToFindFobtException(barcode);
            case 1:
                return await _mediator.Send(new GetFobtByFobtId { FobtId = barcodehistory.First().FOBTId });
            default:
                _logger.LogWarning("Duplicate barcode found for Barcode {Barcode}", barcode);
                throw new DuplicateBarcodeFoundException(barcode);
        }
    }
}