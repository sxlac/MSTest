using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.AkkaStreams.Kafka.Persistence;
using Signify.DEE.Svc.Core.Commands;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Events.Status;
using Signify.DEE.Svc.Core.Infrastructure;
using System;
using System.Threading.Tasks;
using Signify.Dps.Observability.Library.Services;

namespace Signify.DEE.Svc.Core.EventHandlers.Nsb;

public class SaveProviderPayEventHandler(
    ILogger<SaveProviderPayEventHandler> logger,
    IMediator mediator,
    ITransactionSupplier transactionSupplier,
    IApplicationTime applicationTime,
    IMapper mapper,
    IPublishObservability publishObservability)
    : IHandleMessages<SaveProviderPay>
{
    [Transaction]
    public async Task Handle(SaveProviderPay message, IMessageHandlerContext context)
    {
        logger.LogInformation("Starting to handle save provider pay, for EvaluationId={EvaluationId}", message.EvaluationId);

        using var transaction = transactionSupplier.BeginTransaction();
        await WriteToProviderPayTable(message);
        await PublishStatusEvent(message, ExamStatusCode.ProviderPayRequestSent);
        await CommitTransactions(transaction);

        logger.LogInformation("Finished handling save provider pay, for EvaluationId={EvaluationId}", message.EvaluationId);
    }

    /// <summary>
    /// Write the new payment id entry to the ProviderPay table
    /// </summary>
    /// <param name="message"></param>
    /// <param name="examId"></param>
    [Trace]
    private async Task WriteToProviderPayTable(SaveProviderPay message)
    {
        try
        {
            var providerPay = new CreateProviderPay
            {
                ProviderPay = new ProviderPay
                {
                    PaymentId = message.PaymentId,
                    ExamId = message.ExamId,
                    CreatedDateTime = applicationTime.UtcNow(),
                    ProviderPayProductCode = message.ProviderPayProductCode,
                }
            };
            await mediator.Send(providerPay);

            logger.LogInformation("Entry with payment id: {PaymentId} written to {Table} table", message.PaymentId, nameof(ProviderPay));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Entry with payment id: {PaymentId} failed to be written to {Table} table", message.PaymentId, nameof(ProviderPay));
            throw;
        }
    }

    /// <summary>
    /// Invoke ExamStatusEvent as a Mediator event to write to database ExamStatus table with ProviderPayRequestSent and raise kafka event
    /// </summary>
    /// <param name="message"></param>
    /// <param name="statusCode"></param>
    private async Task PublishStatusEvent(SaveProviderPay message, ExamStatusCode statusCode)
    {
        var status = mapper.Map<ProviderPayStatusEvent>(message);
        status.StatusCode = statusCode;
        var updateEvent = new UpdateExamStatus
        {
            ExamStatus = status
        };

        await mediator.Send(updateEvent);
    }

    /// <summary>
    /// Commit both TransactionSupplier and Observability transactions
    /// </summary>
    /// <param name="transaction">The transaction generated as part of TransactionSupplier.BeginTransaction</param>
    [Trace]
    private async Task CommitTransactions(IBufferedTransaction transaction)
    {
        await transaction.CommitAsync();
        publishObservability.Commit();
    }
}