using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.HBA1CPOC.Messages.Events.Status;
using Signify.HBA1CPOC.Svc.Core.Commands;
using Signify.HBA1CPOC.Svc.Core.Data;
using Signify.HBA1CPOC.Svc.Core.Data.Entities;
using Signify.HBA1CPOC.Svc.Core.Infrastructure;
using Signify.HBA1CPOC.Svc.Core.Queries;

namespace Signify.HBA1CPOC.Svc.Core.EventHandlers;

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

        var entity = await GetEntity(message.EvaluationId);

        await WriteToProviderPayTable(message, entity);

        await PublishStatusEvent(message, HBA1CPOCStatusCode.ProviderPayRequestSent, context);

        await transaction.CommitAsync(context.CancellationToken);

        _logger.LogInformation("Finished handling save provider pay, for EvaluationId={EvaluationId}", message.EvaluationId);
    }

    /// <summary>
    /// Read HBA1CPOC entry from database based on the evaluation id
    /// </summary>
    /// <param name="evaluationId"></param>
    /// <returns></returns>
    [Trace]
    private async Task<Data.Entities.HBA1CPOC> GetEntity(long evaluationId)
    {
        var getHba1CPoc = new GetHBA1CPOC
        {
            EvaluationId = (int) evaluationId
        };
        return await _mediator.Send(getHba1CPoc);
    }

    /// <summary>
    /// Write the new payment id entry to the ProviderPay table
    /// </summary>
    /// <param name="message"></param>
    /// <param name="hba1CPoc"></param>
    [Trace]
    private async Task WriteToProviderPayTable(SaveProviderPay message, Data.Entities.HBA1CPOC hba1CPoc)
    {
        try
        {
            var providerPay = new CreateProviderPay
            {
                ProviderPay = new ProviderPay
                {
                    PaymentId = message.PaymentId,
                    HBA1CPOCId = hba1CPoc.HBA1CPOCId,
                    CreatedDateTime = _applicationTime.UtcNow()
                }
            };
            await _mediator.Send(providerPay);

            _logger.LogInformation("Entry with payment id: {PaymentId} written to {Table} table", message.PaymentId,
                nameof(ProviderPay));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Entry with payment id: {PaymentId} failed to be written to {Table} table",
                message.PaymentId, nameof(ProviderPay));
            throw;
        }
    }

   /// <summary>
   /// Invoke <see cref="ExamStatusEvent"/> as a Nsb event to write to database and optionally raise kafka event
   /// </summary>
   /// <param name="message"></param>
   /// <param name="statusCode"></param>
   /// <param name="context"></param>
    [Trace]
    private static async Task PublishStatusEvent(SaveProviderPay message, HBA1CPOCStatusCode statusCode, IMessageHandlerContext context)
    {
        var statusEvent = new ProviderPayStatusEvent
        {
            EvaluationId = message.EvaluationId,
            EventId = message.EventId,
            StatusCode = statusCode.HBA1CPOCStatusCodeId,
            StatusDateTime = message.PdfDeliveryDateTime,
            ParentCdiEvent = message.GetType().Name,
            PaymentId = message.PaymentId
        };
        await context.SendLocal(statusEvent);
    }
}
