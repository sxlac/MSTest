using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.uACR.Core.Commands;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Data;
using Signify.uACR.Core.Events.Status;
using Signify.uACR.Core.Infrastructure;
using System.Threading;
using System;
using NsbEventHandlers;
using Signify.Dps.Observability.Library.Services;
using UacrNsbEvents;
using Task = System.Threading.Tasks.Task;

namespace Signify.uACR.Core.EventHandlers.Nsb;

public class SaveProviderPayEventHandler(
    ILogger<SaveProviderPayEventHandler> logger,
    IMediator mediator,
    IMapper mapper,
    ITransactionSupplier transactionSupplier,
    IApplicationTime applicationTime,
    IPublishObservability publishObservability)
    : BaseNsbHandler(logger, mediator, transactionSupplier, publishObservability, applicationTime), IHandleMessages<SaveProviderPay>
{
    [Transaction]
    public async Task Handle(SaveProviderPay message, IMessageHandlerContext context)
    {
        Logger.LogInformation("Starting to handle save provider pay, for EvaluationId={EvaluationId}",
            message.EvaluationId);

        using var transaction = TransactionSupplier.BeginTransaction();
        
        // update database - ProviderPay table
        await WriteToProviderPayTable(message, context.CancellationToken);
        await PublishStatusEvent(message, ExamStatusCode.ProviderPayRequestSent, context.CancellationToken);
        
        await CommitTransactions(transaction, context.CancellationToken);

        Logger.LogInformation("Finished handling save provider pay, for EvaluationId={EvaluationId}",
            message.EvaluationId);
    }

    /// <summary>
    /// Invoke ExamStatusEvent as a Mediator event to write to database StatusCode table with ProviderPayRequestSent and raise kafka event
    /// </summary>
    /// <param name="message"></param>
    /// <param name="statusCode"></param>
    /// <param name="token"></param>
    private async Task PublishStatusEvent(SaveProviderPay message, ExamStatusCode statusCode, CancellationToken token)
    {
        var status = mapper.Map<ProviderPayStatusEvent>(message);
        status.StatusCode = statusCode;
        var updateEvent = new UpdateExamStatus
        {
            ExamStatus = status
        };
            
        await Mediator.Send(updateEvent, token);
    }

    /// <summary>
    /// Write the new payment id entry to the ProviderPay table
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    [Trace]
    private async Task WriteToProviderPayTable(SaveProviderPay message, CancellationToken cancellationToken)
    {
        var providerPay = new AddProviderPay
        {
            ProviderPay = new ProviderPay
            {
                PaymentId = message.PaymentId,
                ExamId = message.ExamId,
                CreatedDateTime = new DateTimeOffset(ApplicationTime.UtcNow())
            }
        };
        await Mediator.Send(providerPay, cancellationToken);

        Logger.LogInformation("Propose to commit entry with payment id: {PaymentId} to {Table} table",
            message.PaymentId,
            nameof(ProviderPay));
    }
}