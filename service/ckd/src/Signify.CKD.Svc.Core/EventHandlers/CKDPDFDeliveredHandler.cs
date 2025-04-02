using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.CKD.Messages.Events;
using Signify.CKD.Svc.Core.Commands;
using Signify.CKD.Svc.Core.Constants;
using Signify.CKD.Svc.Core.Data;
using Signify.CKD.Svc.Core.Data.Entities;
using Signify.CKD.Svc.Core.Events;
using Signify.CKD.Svc.Core.Exceptions;
using Signify.CKD.Svc.Core.FeatureFlagging;
using Signify.CKD.Svc.Core.Messages.Status;
using Signify.CKD.Svc.Core.Queries;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Signify.CKD.Svc.Core.EventHandlers;

public class CKDPdfDeliveredHandler : IHandleMessages<PdfDeliveredToClient>
{
    private readonly ILogger<CKDPdfDeliveredHandler> _logger;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly IFeatureFlags _featureFlags;
    private readonly IAgent _newRelicAgent;
    private readonly ITransactionSupplier _transactionSupplier;

    public CKDPdfDeliveredHandler(ILogger<CKDPdfDeliveredHandler> logger,
        IMapper mapper,
        IMediator mediator,
        IFeatureFlags featureFlags,
        IAgent newRelicAgent,
        ITransactionSupplier transactionSupplier)
    {
        _logger = logger;
        _mapper = mapper;
        _mediator = mediator;
        _featureFlags = featureFlags;
        _newRelicAgent = newRelicAgent;
        _transactionSupplier = transactionSupplier;
    }

    [Transaction]
    public async Task Handle(PdfDeliveredToClient message, IMessageHandlerContext context)
    {
        _logger.LogDebug("Start Handle {Handler}, EvaluationID: {EvaluationId}, EventId: {EventId}",
            nameof(CKDPdfDeliveredHandler), message.EvaluationId, message.EventId);

        // Query database to check if ckd exists.
        var ckd = await _mediator.Send(new GetCKD { EvaluationId = message.EvaluationId });
        if (ckd == null)
            throw new ExamNotFoundException(message.EvaluationId, message.EventId);

        var isPerformed = await IsPerformed(message, ckd);

        using var transaction = _transactionSupplier.BeginTransaction();

        // Get or create a PDF Entry for this exam
        var pdf = await UpdatePdfToClientTable(isPerformed, ckd, message);

        //ANC-2678 Has to have a valid Answer as well for billing
        var isValidCkdAnswer = !string.IsNullOrWhiteSpace(ckd.CKDAnswer);

        if (isPerformed && isValidCkdAnswer)
        {
            await UpdateCkdStatusTable(ckd, CKDStatusCode.BillableEventRecieved);
            await SendBillingRequest(message, ckd, pdf, context);
        }
        else
        {
            await UpdateCkdStatusTable(ckd, CKDStatusCode.BillRequestNotSent);

            var kafkaBillRequestNotSentMessage = _mapper.Map<BillRequestNotSent>(ckd);
            kafkaBillRequestNotSentMessage.PdfDeliveryDate = message.DeliveryDateTime;
            await _mediator.Send(new PublishStatusUpdate(kafkaBillRequestNotSentMessage));
        }

        await transaction.CommitAsync();
        _logger.LogInformation("Finished handling event for EvaluationId={EvaluationId}", message.EvaluationId);
    }

    private async Task<bool> IsPerformed(PdfDeliveredToClient message, Data.Entities.CKD ckd)
    {
        var statuses = await _mediator.Send(new GetCKDStatuses { CKDId = ckd.CKDId });

        foreach (var status in statuses)
        {
            // Unfortunately can't use a switch since these aren't constants
            if (status.CKDStatusCode.CKDStatusCodeId == CKDStatusCode.CKDPerformed.CKDStatusCodeId)
                return true;

            if (status.CKDStatusCode.CKDStatusCodeId == CKDStatusCode.CKDNotPerformed.CKDStatusCodeId)
                return false;
        }

        throw new UnableToDetermineBillabilityException(message.EventId, message.EvaluationId);
    }

    /// <summary>
    /// Update CkdStatus table
    /// </summary>
    /// <param name="ckd"></param>
    /// <param name="statusCode">Status code to add</param>
    [Trace]
    private Task UpdateCkdStatusTable(Data.Entities.CKD ckd, CKDStatusCode statusCode)
    {
        return _mediator.Send(new CreateCKDStatus
        {
            CKDId = ckd.CKDId,
            StatusCodeId = statusCode.CKDStatusCodeId
        });
    }

    /// <summary>
    /// Update PdfToClient table
    /// </summary>
    /// <param name="isPerformed"></param>
    /// <param name="ckd"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    [Trace]
    private Task<PDFToClient> UpdatePdfToClientTable(bool isPerformed, Data.Entities.CKD ckd, PdfDeliveredToClient message)
    {
        return _mediator.Send(new InsertPdfToClientTransaction
        {
            CKDId = ckd.CKDId,
            PdfDeliveredToClient = message,
            EvaluationId = message.EvaluationId,
            IsPerformed = isPerformed
        });
    }

    /// <summary>
    /// Send NSB event to handle request to RCM billing API, database write and kafka event
    /// </summary>
    /// <param name="message"></param>
    /// <param name="ckd"></param>
    /// <param name="pdf"></param>
    /// <param name="context"></param>
    [Trace]
    private Task SendBillingRequest(PdfDeliveredToClient message, Data.Entities.CKD ckd, PDFToClient pdf, IPipelineContext context)
    {
        var rcmBillingRequest = _mapper.Map<RCMBillingRequest>(ckd);
        rcmBillingRequest.ApplicationId = "signify.ckd.service";
        rcmBillingRequest.RcmProductCode = Application.ProductCode;
        rcmBillingRequest.BillableDate = message.DeliveryDateTime;
        rcmBillingRequest.CorrelationId = Guid.NewGuid().ToString();
        rcmBillingRequest.PdfDeliveryDateTime = pdf.DeliveryDateTime;
        rcmBillingRequest.AdditionalDetails = new Dictionary<string, string>
        {
            { "BatchName", message.BatchName },
            { "EvaluationId", rcmBillingRequest.EvaluationId.ToString() },
            { "appointmentId", ckd.AppointmentId.ToString() }
        };

        return context.SendLocal(rcmBillingRequest);
    }
}