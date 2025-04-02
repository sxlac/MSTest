using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Data;
using Signify.PAD.Svc.Core.Events;
using Signify.PAD.Svc.Core.Queries;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Signify.PAD.Svc.Core.Exceptions;
using Signify.PAD.Svc.Core.Models;

using Pad = Signify.PAD.Svc.Core.Data.Entities.PAD;

namespace Signify.PAD.Svc.Core.EventHandlers;

public class PadPdfDeliveredHandler : IHandleMessages<PdfDeliveredToClient>
{
    private readonly ILogger _logger;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly ITransactionSupplier _transactionSupplier;

    public PadPdfDeliveredHandler(ILogger<PadPdfDeliveredHandler> logger,
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
    public async Task Handle(PdfDeliveredToClient message, IMessageHandlerContext context)
    {
        if (await HasPdfDeliveryEvent(message.EvaluationId, context.CancellationToken))
        {
            _logger.LogInformation("Already processed a PdfDeliveredToClient event for EvaluationId={EvaluationId}, ignoring EventId={EventId}", message.EvaluationId, message.EventId);
            return;
        }

        _logger.LogInformation("Received PdfDeliveredToClient with EventId={EventId}, for EvaluationId={EvaluationId}", message.EventId, message.EvaluationId);

        using var transaction = _transactionSupplier.BeginTransaction();

        var pad = await GetExam(message, context.CancellationToken);

        await SavePdfDelivered(message, pad, context.CancellationToken);

        if (!await IsPerformed(message, pad, context.CancellationToken))
        {
            await SendStatus(StatusCodes.BillRequestNotSent);
            await transaction.CommitAsync(context.CancellationToken);
            return;
        }

        await SendStatus(StatusCodes.BillableEventReceived);
        await SendBillingRequest(message, pad, context);
        await transaction.CommitAsync(context.CancellationToken);

        _logger.LogDebug("Finished processing event for EvaluationId={EvaluationId}, EventId={EventId}",
            message.EvaluationId, message.EventId);

        Task SendStatus(StatusCodes statusCode)
        {
            return _mediator.Send(new ExamStatusEventNew
            {
                EventId = message.EventId,
                Exam = pad,
                StatusCode = statusCode,
                StatusDateTime = message.CreatedDateTime
            }, context.CancellationToken);
        }
    }

    private async Task<bool> HasPdfDeliveryEvent(long evaluationId, CancellationToken cancellationToken)
    {
        return (await _mediator.Send(new QueryPdfDeliveredToClient(evaluationId), cancellationToken)).Entity != null;
    }

    private async Task<Pad> GetExam(PdfDeliveredToClient message, CancellationToken cancellationToken)
    {
        var pad = await _mediator.Send(new GetPAD { EvaluationId = message.EvaluationId }, cancellationToken);
        if (pad == null)
            throw new ExamNotFoundException(message.EvaluationId, message.EventId);

        return pad;
    }

    private async Task<bool> IsPerformed(PdfDeliveredToClient message, Pad pad, CancellationToken cancellationToken)
    {
        var status = await _mediator.Send(new QueryPadPerformedStatus(pad.PADId), cancellationToken);
        if (!status.IsPerformed.HasValue)
            throw new UnableToDetermineBillabilityException(message.EventId, message.EvaluationId);

        return status.IsPerformed.Value;
    }

    [Trace]
    private async Task SavePdfDelivered(PdfDeliveredToClient message, Data.Entities.PAD pad, CancellationToken cancellationToken)
    {
        var pdfDeliveryReceived = _mapper.Map<CreateOrUpdatePDFToClient>(message);
        pdfDeliveryReceived.PADId = pad.PADId;

        await _mediator.Send(pdfDeliveryReceived, cancellationToken);
    }

    [Trace]
    private Task SendBillingRequest(PdfDeliveredToClient message, Pad pad, IMessageHandlerContext context)
    {
        var rcmBillingRequest = _mapper.Map<RcmBillingRequest>(pad);
        rcmBillingRequest.BillableDate = message.DeliveryDateTime;
        rcmBillingRequest.CorrelationId = Guid.NewGuid().ToString();
        rcmBillingRequest.AdditionalDetails = new Dictionary<string, string>
        {
            { "BatchName", message.BatchName },
            { "EvaluationId", rcmBillingRequest.EvaluationId.ToString() },
            { "appointmentId", pad.AppointmentId.ToString() } // note unlike the others, must be lower-case "appointment"
        };

        return context.SendLocal(rcmBillingRequest);
    }
}
