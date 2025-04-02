using AutoMapper;
using EgfrNsbEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.AkkaStreams.Kafka.Persistence;
using Signify.eGFR.Core.Commands;
using Signify.eGFR.Core.Constants;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Data;
using Signify.eGFR.Core.Events.Status;
using Signify.eGFR.Core.Infrastructure.Vendor;
using System.Threading.Tasks;
using System.Threading;
using Signify.Dps.Observability.Library.Services;
using Signify.eGFR.Core.Infrastructure;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers;

/// <summary>
/// NSB event handler for the <see cref="OrderCreationEvent"/>
/// </summary>
public class OrderCreationEventHandler(
    ILogger<OrderCreationEventHandler> logger,
    IMediator mediator,
    ITransactionSupplier transactionSupplier,
    IPublishObservability publishObservability,
    IApplicationTime applicationTime,
    IMapper mapper)
    : BaseNsbHandler(logger, mediator, transactionSupplier, publishObservability, applicationTime), IHandleMessages<OrderCreationEvent>
{
    [Transaction]
    public async Task Handle(OrderCreationEvent message, IMessageHandlerContext context)
    {
        Logger.LogInformation("Handling OrderCreation Event EvaluationId={EvaluationId}",
            message.EvaluationId);
        using var transaction = TransactionSupplier.BeginTransaction();
        var orderCreationEvent = mapper.Map<Signify.eGFR.Core.Events.Akka.OrderCreationEvent>(message);
        
        // Publish order creation event to Kafka
        await Mediator.Send(new PublishOrderCreation(orderCreationEvent, orderCreationEvent.EventId), context.CancellationToken);

        // Publish exam status to DB and update new relic
        await PublishStatusEvent(message, context.CancellationToken);
        
        await CommitTransactions(transaction, context.CancellationToken);
        Logger.LogInformation("Finished handling OrderCreation Event EvaluationId={EvaluationId}",
            message.EvaluationId);
    }

    /// <summary>
    /// Publish OrderCreationEventSent status to DB
    /// </summary>
    /// <param name="message"></param>
    /// <param name="token"></param>
    private async Task PublishStatusEvent(OrderCreationEvent message, CancellationToken token)
    {
        var barcode = "";
        //For each vendor search context for their barcode:
        if (VendorDetermination.Vendor.LetsGetChecked.ToString().Equals(message.Vendor))
        {
            if (message.Context != null && message.Context.TryGetValue(Vendor.LgcBarcode, out var outValue))
                barcode = outValue;
            if (message.Context != null && message.Context.TryGetValue(Vendor.LgcAlphaCode, out var outValue2))
                barcode = barcode+"|"+outValue2;
        }
        var status = new OrderRequestedStatusEvent
        {
            EvaluationId = message.EvaluationId,
            ExamId = message.ExamId,
            EventId = message.EventId,
            StatusDateTime = message.StatusDateTime,
            StatusCode = ExamStatusCode.OrderRequested,
            Barcode = barcode,
            Vendor = message.Vendor
        };

        var updateEvent = new UpdateExamStatus
        {
            ExamStatus = status
        };
        await Mediator.Send(updateEvent, token);
    }
}