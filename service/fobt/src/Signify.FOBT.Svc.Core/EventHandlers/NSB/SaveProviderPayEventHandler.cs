using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.FOBT.Svc.Core.Commands;
using Signify.FOBT.Svc.Core.Data;
using Signify.FOBT.Svc.Core.Data.Entities;
using Signify.FOBT.Svc.Core.Infrastructure;
using Signify.FOBT.Svc.Core.Infrastructure.Observability;
using Signify.FOBT.Svc.Core.Models;
using Signify.FOBT.Svc.Core.Queries;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers;

public class SaveProviderPayEventHandler : IHandleMessages<SaveProviderPay>
{
    private readonly ILogger<SaveProviderPayEventHandler> _logger;
    private readonly IMediator _mediator;
    private readonly ITransactionSupplier _transactionSupplier;
    private readonly IApplicationTime _applicationTime;
    private readonly IPublishObservability _publishObservability;

    public SaveProviderPayEventHandler(ILogger<SaveProviderPayEventHandler> logger, IMediator mediator,
        ITransactionSupplier transactionSupplier, IApplicationTime applicationTime, IPublishObservability publishObservability)
    {
        _logger = logger;
        _mediator = mediator;
        _transactionSupplier = transactionSupplier;
        _applicationTime = applicationTime;
        _publishObservability = publishObservability;
    }

    [Transaction]
    public async Task Handle(SaveProviderPay message, IMessageHandlerContext context)
    {
        _logger.LogInformation("Starting to handle save provider pay, for EvaluationId={EvaluationId}",
            message.EvaluationId);

        using var transaction = _transactionSupplier.BeginTransaction();
        var fobt = await ReadFobtFromDatabase(message.EvaluationId);
        // update database - ProviderPay table
        await WriteToProviderPayTable(message, fobt);
        await PublishStatusEvent(message, fobt, FOBTStatusCode.ProviderPayRequestSent);
        await transaction.CommitAsync(context.CancellationToken);
        _publishObservability.Commit();

        _logger.LogInformation("Finished handling save provider pay, for EvaluationId={EvaluationId}",
            message.EvaluationId);
    }

    /// <summary>
    /// Invoke ExamStatusEvent as a Mediator event to write to database FOBTStatus table with ProviderPayRequestSent and raise kafka event
    /// </summary>
    /// <param name="message"></param>
    /// <param name="fobt"></param>
    /// <param name="statusCode"></param>
    private async Task PublishStatusEvent(SaveProviderPay message, FOBT fobt, FOBTStatusCode statusCode)
    {
        var statusEvent = new UpdateExamStatus
        {
            ExamStatus = new ProviderPayStatusEvent
            {
                Exam = fobt,
                EvaluationId = message.EvaluationId,
                EventId = message.EventId,
                StatusCode = statusCode,
                StatusDateTime = message.ParentEventDateTime,
                PaymentId = message.PaymentId,
                ParentEventReceivedDateTime = message.ParentEventReceivedDateTime
            }
        };
        await _mediator.Send(statusEvent);
    }

    /// <summary>
    /// Write the new payment id entry to the ProviderPay table
    /// </summary>
    /// <param name="message"></param>
    /// <param name="fobt"></param>
    [Trace]
    private async Task WriteToProviderPayTable(SaveProviderPay message, FOBT fobt)
    {
        var providerPay = new CreateProviderPay
        {
            ProviderPay = new ProviderPay
            {
                PaymentId = message.PaymentId,
                FOBTId = fobt.FOBTId,
                CreatedDateTime = new DateTimeOffset(_applicationTime.UtcNow())
            }
        };
        await _mediator.Send(providerPay);

        _logger.LogInformation("Propose to commit entry with payment id: {PaymentId} to {Table} table",
            message.PaymentId,
            nameof(ProviderPay));
    }

    /// <summary>
    /// Read FOBT entry from database based on the evaluation id
    /// </summary>
    /// <param name="evaluationId"></param>
    /// <returns></returns>
    [Trace]
    private async Task<FOBT> ReadFobtFromDatabase(long evaluationId)
    {
        var getFobt = new GetFOBT
        {
            EvaluationId = (int)evaluationId
        };
        var fobt = await _mediator.Send(getFobt);
        return fobt;
    }
}