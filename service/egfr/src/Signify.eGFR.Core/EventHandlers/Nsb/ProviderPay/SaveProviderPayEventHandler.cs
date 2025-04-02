using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.AkkaStreams.Kafka.Persistence;
using Signify.eGFR.Core.Commands;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Data;
using Signify.eGFR.Core.Events.Status;
using Signify.eGFR.Core.Infrastructure;
using System.Threading.Tasks;
using System.Threading;
using System;
using Signify.Dps.Observability.Library.Services;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers;

public class SaveProviderPayEventHandler(
    ILogger<SaveProviderPayEventHandler> logger,
    IMediator mediator,
    ITransactionSupplier transactionSupplier,
    IPublishObservability publishObservability,
    IApplicationTime applicationTime,
    IMapper mapper)
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
    /// <param name="token"></param>
    [Trace]
    private async Task WriteToProviderPayTable(SaveProviderPay message, CancellationToken token)
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
        await Mediator.Send(providerPay, token);

        Logger.LogInformation("Propose to commit entry with payment id: {PaymentId} to {Table} table",
            message.PaymentId,
            nameof(ProviderPay));
    }
}