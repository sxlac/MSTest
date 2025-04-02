using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.HBA1CPOC.Messages.Events;
using Signify.HBA1CPOC.Svc.Core.Commands;
using Signify.HBA1CPOC.Svc.Core.Constants;
using Signify.HBA1CPOC.Svc.Core.Data;
using Signify.HBA1CPOC.Svc.Core.Data.Entities;
using Signify.HBA1CPOC.Svc.Core.Exceptions;
using Signify.HBA1CPOC.Svc.Core.Queries;

namespace Signify.HBA1CPOC.Svc.Core.EventHandlers;

public class A1cPocPdfDeliveredHandler : IHandleMessages<PdfDeliveredToClient>
{
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;
    private readonly ITransactionSupplier _transactionSupplier;

    public A1cPocPdfDeliveredHandler(ILogger<A1cPocPdfDeliveredHandler> logger,
        IMapper mapper,
        IMediator mediator,
        ITransactionSupplier transactionSupplier)
    {
        _logger = logger;
        _mapper = mapper;
        _mediator = mediator;
        _transactionSupplier = transactionSupplier;
    }

    [Transaction]
    public async Task Handle(PdfDeliveredToClient pdfDeliveredToClientMessage, IMessageHandlerContext context)
    {
        _logger.LogInformation("Starting to handle pdf delivery event for EvaluationId={EvaluationId} with EventId={EventId}",
            pdfDeliveredToClientMessage.EvaluationId, pdfDeliveredToClientMessage.EventId);

        using var transaction = _transactionSupplier.BeginTransaction();

        //Query database to check if A1C exists.
        var exam = await _mediator.Send(new GetHBA1CPOC { EvaluationId = pdfDeliveredToClientMessage.EvaluationId }, context.CancellationToken);
        if (exam == null)
        {
            throw new ExamNotFoundException(pdfDeliveredToClientMessage.EvaluationId,
                pdfDeliveredToClientMessage.EventId);
        }

        var pdfEntry = await _mediator.Send(new GetPdfToClient { EvaluationId = pdfDeliveredToClientMessage.EvaluationId }, context.CancellationToken);

        if (pdfEntry == null)
        {
            await _mediator.Send(new InsertPdfToClientTransaction
            {
                PdfDeliveredToClient = pdfDeliveredToClientMessage,
                HbA1cPoc = exam
            }, context.CancellationToken);
        }

        if (!await WasExamPerformed(pdfDeliveredToClientMessage.EvaluationId))
        {
            _logger.LogInformation("EvaluationId:{EvaluationId} doesn't contain Performed status, not sending to RCM",
                pdfDeliveredToClientMessage.EvaluationId);
            await transaction.CommitAsync(context.CancellationToken);
            return;
        }

        await UpdateStatusTable(exam, HBA1CPOCStatusCode.BillableEventRecieved);
        await SendBillingRequest(pdfDeliveredToClientMessage, exam, context);
        await transaction.CommitAsync(context.CancellationToken);
    }

    /// <summary>
    /// Check if HBA1CPOC exam was performed for the <see cref="evaluationId"/>
    /// </summary>
    /// <param name="evaluationId"></param>
    /// <returns></returns>
    private async Task<bool> WasExamPerformed(long evaluationId)
    {
        var status = await _mediator.Send(new GetHba1CPocStatus
        {
            EvaluationId = evaluationId,
            StatusCode = HBA1CPOCStatusCode.HBA1CPOCPerformed
        });

        return status != null;
    }

    /// <summary>
    /// Send NSB event to handle request to RCM billing API, database write and kafka event
    /// </summary>
    /// <param name="message"></param>
    /// <param name="exam"></param>
    /// <param name="context"></param>
    [Trace]
    private async Task SendBillingRequest(PdfDeliveredToClient message, Data.Entities.HBA1CPOC exam, IPipelineContext context)
    {
        var rcmBillingRequest = _mapper.Map<RCMBillingRequest>(exam);
        rcmBillingRequest.EventId = message.EventId;
        rcmBillingRequest.ApplicationId = "signify.hba1cpoc.service";
        rcmBillingRequest.RcmProductCode = ApplicationConstants.ProductCode;
        rcmBillingRequest.BillableDate = message.DeliveryDateTime;
        rcmBillingRequest.CorrelationId = Guid.NewGuid().ToString();
        rcmBillingRequest.AdditionalDetails = new Dictionary<string, string>()
        {
            { "BatchName", message.BatchName },
            { "EvaluationId", rcmBillingRequest.EvaluationId.ToString() },
            { "appointmentId", exam.AppointmentId.ToString() }
        };

        await context.SendLocal(rcmBillingRequest);
    }

    /// <summary>
    /// Update CkdStatus table
    /// </summary>
    /// <param name="exam"></param>
    /// <param name="statusCode">Status code to add</param>
    [Trace]
    private async Task UpdateStatusTable(Data.Entities.HBA1CPOC exam, HBA1CPOCStatusCode statusCode)
    {
        await _mediator.Send(new CreateHBA1CPOCStatus
        {
            HBA1CPOCId = exam.HBA1CPOCId,
            StatusCodeId = statusCode.HBA1CPOCStatusCodeId
        });
    }
}
