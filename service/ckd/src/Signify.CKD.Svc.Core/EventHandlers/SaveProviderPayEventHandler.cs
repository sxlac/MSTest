using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.CKD.Svc.Core.Commands;
using Signify.CKD.Svc.Core.Data;
using Signify.CKD.Svc.Core.Data.Entities;
using Signify.CKD.Svc.Core.Events;
using Signify.CKD.Svc.Core.Infrastructure;
using Signify.CKD.Svc.Core.Queries;

namespace Signify.CKD.Svc.Core.EventHandlers;

public class SaveProviderPayEventHandler : IHandleMessages<SaveProviderPay>
{
    private readonly ILogger _logger;
    private readonly IMediator _mediator;
    private readonly ITransactionSupplier _transactionSupplier;
    private readonly IApplicationTime _applicationTime;

    public SaveProviderPayEventHandler(ILogger<SaveProviderPayEventHandler> logger,
        IMediator mediator,
        ITransactionSupplier transactionSupplier,
        IApplicationTime applicationTime)
    {
        _logger = logger;
        _mediator = mediator;
        _transactionSupplier = transactionSupplier;
        _applicationTime = applicationTime;
    }

    [Transaction]
    public async Task Handle(SaveProviderPay message, IMessageHandlerContext context)
    {
        _logger.LogInformation("Starting to handle save provider pay, for EvaluationId={EvaluationId}", message.EvaluationId);

        using var transaction = _transactionSupplier.BeginTransaction();
        var ckd = await _mediator.Send(new GetCKD{EvaluationId = message.EvaluationId});
        await WriteToProviderPayTable(message, ckd);
        await PublishStatusEvent(message, CKDStatusCode.ProviderPayRequestSent, context);
        await transaction.CommitAsync();

        _logger.LogInformation("Finished handling save provider pay, for EvaluationId={EvaluationId}", message.EvaluationId);
    }
    
    /// <summary>
    /// Invoke <see cref="ExamStatusEvent"/> as a Nsb event to write to database and optionally raise kafka event
    /// </summary>
    /// <param name="message"></param>
    /// <param name="examId"></param>
    /// <param name="statusCode"></param>
    /// <param name="context">IMessageHandlerContext</param>
    private static Task PublishStatusEvent(SaveProviderPay message, CKDStatusCode statusCode, IMessageHandlerContext context)
    {
        var statusEvent = new ProviderPayStatusEvent
        {
            EvaluationId = message.EvaluationId,
            EventId = message.EventId,
            StatusCode = statusCode,
            StatusDateTime = message.PdfDeliveryDateTime,
            ParentCdiEvent = message.GetType().Name,
            PaymentId = message.PaymentId
        };

        return context.SendLocal(statusEvent);
    }

    /// <summary>
    /// Write the new payment id entry to the ProviderPay table
    /// </summary>
    /// <param name="message"></param>
    /// <param name="ckd"></param>
    [Trace]
    private async Task WriteToProviderPayTable(SaveProviderPay message, Data.Entities.CKD ckd)
    {
        try
        {
            var providerPay = new CreateProviderPay
            {
                ProviderPay = new ProviderPay
                {
                    PaymentId = message.PaymentId,
                    CKDId = ckd.CKDId,
                    CreatedDateTime = _applicationTime.UtcNow()
                }
            };
            await _mediator.Send(providerPay);

            _logger.LogInformation("Entry with payment id: {PaymentId} written to {Table} table", message.PaymentId, nameof(ProviderPay));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Entry with payment id: {PaymentId} failed to be written to {Table} table", message.PaymentId, nameof(ProviderPay));
            throw;
        }
    }
}