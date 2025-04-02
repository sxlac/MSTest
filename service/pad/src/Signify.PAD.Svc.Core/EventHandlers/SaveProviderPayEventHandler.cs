using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Data;
using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.Events;
using Signify.PAD.Svc.Core.Infrastructure;
using Signify.PAD.Svc.Core.Queries;

namespace Signify.PAD.Svc.Core.EventHandlers;

public class SaveProviderPayEventHandler : IHandleMessages<SaveProviderPay>
{
    private readonly ILogger<SaveProviderPayEventHandler> _logger;
    private readonly IMediator _mediator;
    private readonly ITransactionSupplier _transactionSupplier;
    private readonly IApplicationTime _applicationTime;

    public SaveProviderPayEventHandler(ILogger<SaveProviderPayEventHandler> logger, IMediator mediator,
        ITransactionSupplier transactionSupplier, IApplicationTime applicationTime)
    {
        _logger = logger;
        _mediator = mediator;
        _transactionSupplier = transactionSupplier;
        _applicationTime = applicationTime;
    }

    [Transaction]
    public async Task Handle(SaveProviderPay message, IMessageHandlerContext context)
    {
        using var scope = _logger.BeginScope("EventId={EventId}, EvaluationId={EvaluationId}", message.EventId, message.EvaluationId);
        _logger.LogDebug("Start Handle of {HandlerType}", nameof(SaveProviderPay));

        using var transaction = _transactionSupplier.BeginTransaction();
        var pad = await ReadPadFromDatabase(message.EvaluationId, context.CancellationToken);
        await WriteToProviderPayTable(message, pad, context.CancellationToken);
        await PublishStatusEventAsync(context, message, pad.PADId, PADStatusCode.ProviderPayRequestSent);
        
        await transaction.CommitAsync(context.CancellationToken);

        _logger.LogDebug("End Handle of {HandlerType}", nameof(SaveProviderPay));
    }

    /// <summary>
    /// Invoke <see cref="ExamStatusEvent"/> as a Nsb event to write to database and optionally raise kafka event
    /// </summary>
    /// <param name="message"></param>
    /// <param name="examId"></param>
    /// <param name="statusCode"></param>
    /// <param name="context">IMessageHandlerContext</param>
    [Trace]
    private static async Task PublishStatusEventAsync(IMessageHandlerContext context, SaveProviderPay message, int examId, PADStatusCode statusCode)
    {
        var statusEvent = new ProviderPayStatusEvent
        {
            EvaluationId = message.EvaluationId,
            EventId = message.EventId,
            ExamId = examId,
            StatusCode = statusCode,
            StatusDateTime = message.PdfDeliveryDateTime,
            ParentCdiEvent = message.GetType().Name,
            PaymentId = message.PaymentId
        };

        await context.SendLocal(statusEvent);
    }

    /// <summary>
    /// Write the new payment id entry to the ProviderPay table
    /// </summary>
    /// <param name="message"></param>
    /// <param name="pad"></param>
    [Trace]
    private async Task WriteToProviderPayTable(SaveProviderPay message, Data.Entities.PAD pad, CancellationToken cancellationToken)
    {
        try
        {
            var providerPay = new CreateProviderPay
            {
                ProviderPay = new ProviderPay
                {
                    PaymentId = message.PaymentId,
                    PADId = pad.PADId,
                    CreatedDateTime = new DateTimeOffset(_applicationTime.UtcNow())
                }
            };
            await _mediator.Send(providerPay, cancellationToken);

            _logger.LogInformation("Entry with payment id: {PaymentId} written to {Table} table", message.PaymentId, nameof(ProviderPay));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Entry with payment id: {PaymentId} failed to be written to {Table} table", message.PaymentId, nameof(ProviderPay));
            throw;
        }
    }

    /// <summary>
    /// Read PAD entry from database based on the evaluation id
    /// </summary>
    /// <param name="evaluationId"></param>
    /// <returns></returns>
    [Trace]
    private Task<Data.Entities.PAD> ReadPadFromDatabase(long evaluationId, CancellationToken cancellationToken)
    {
        var getPad = new GetPAD
        {
            EvaluationId = evaluationId
        };
        return _mediator.Send(getPad, cancellationToken);
    }
}