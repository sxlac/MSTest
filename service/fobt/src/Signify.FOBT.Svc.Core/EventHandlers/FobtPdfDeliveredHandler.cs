using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Signify.FOBT.Messages.Events.Status;
using Signify.FOBT.Svc.Core.Commands;
using Signify.FOBT.Svc.Core.Constants;
using Signify.FOBT.Svc.Core.Data.Entities;
using Signify.FOBT.Svc.Core.Events;
using Signify.FOBT.Svc.Core.Messages.Queries;
using Signify.FOBT.Svc.Core.Queries;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Signify.FOBT.Svc.Core.BusinessRules;
using Signify.FOBT.Svc.Core.Exceptions;
using Signify.FOBT.Svc.Core.Models;
using Fobt = Signify.FOBT.Svc.Core.Data.Entities.FOBT;

namespace Signify.FOBT.Svc.Core.EventHandlers;

public class FobtPdfDeliveredHandler : IHandleMessages<PdfDeliveredToClient>
{
    private const string LeftBehindBillingProductCode = ApplicationConstants.BILLING_PRODUCT_CODE_LEFT;
    private const string ResultsBillingProductCode = ApplicationConstants.BILLING_PRODUCT_CODE_RESULTS;

    private readonly ILogger<FobtPdfDeliveredHandler> _logger;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly IBillableRules _billableRules;

    public FobtPdfDeliveredHandler(ILogger<FobtPdfDeliveredHandler> logger, IMapper mapper, IMediator mediator, IBillableRules billableRules)
    {
        _logger = logger;
        _mapper = mapper;
        _mediator = mediator;
        _billableRules = billableRules;
    }

    public async Task Handle(PdfDeliveredToClient pdfDeliveredToClientMessage, IMessageHandlerContext context)
    {
        using var scope = _logger.BeginScope("EventId={EventId}, EvaluationId={EvaluationId}",
            pdfDeliveredToClientMessage.EventId, pdfDeliveredToClientMessage.EvaluationId);

        var evaluationId = (int)pdfDeliveredToClientMessage.EvaluationId;

        // Query Db to check if FOBT entry exists
        var fobt = await _mediator.Send(new GetFOBT { EvaluationId = evaluationId }, context.CancellationToken);
        if (fobt == null)
        {
            _logger.LogError("Evaluation not found in database");
            throw new ExamNotFoundException(pdfDeliveredToClientMessage.EvaluationId, pdfDeliveredToClientMessage.EventId);
        }

        // Check FOBT Performed status
        var fobtPerformedStatusResult = await _mediator.Send(new GetFobtStatusByStatusCodeAndEvaluationId
        {
            EvaluationId = evaluationId,
            FobtStatusCode = FOBTStatusCode.FOBTPerformed
        }, context.CancellationToken);

        if (fobtPerformedStatusResult?.FOBT?.EvaluationId != evaluationId)
        {
            _logger.LogInformation("Evaluation doesn't contain Performed status, not sending to RCM");
            return;
        }

        var pdfEntry = await _mediator.Send(new GetPDFToClient(fobt.FOBTId, evaluationId), context.CancellationToken);
        if (pdfEntry == null)
        {
            await _mediator.Send(new InsertPdfToClient
            {
                PdfDeliveredToClient = pdfDeliveredToClientMessage,
                Fobt = fobt
            }, context.CancellationToken);
        }

        // Send Request to Queue
        await SendBillingRequest(pdfDeliveredToClientMessage, fobt, LeftBehindBillingProductCode, FOBTStatusCode.LeftBehindBillRequestSent, context);

        if (await CanBillForResults(fobt, evaluationId))
        {
            await SendBillingRequest(pdfDeliveredToClientMessage, fobt, ResultsBillingProductCode, FOBTStatusCode.ResultsBillRequestSent, context);
        }
        else
        {
            //Publish bill request not sent event and add status
            var labResults = await _mediator.Send(new GetLabResultByFobtId
            {
                FobtId = fobt.FOBTId
            }, context.CancellationToken);

            if (labResults != null)
            {
                var publishBillRequestNotSent = _mapper.Map<BillRequestNotSent>(fobt);
                publishBillRequestNotSent.ReceivedDate = labResults.CreatedDateTime ?? DateTime.UtcNow;
                await _mediator.Send(new PublishStatusUpdate(publishBillRequestNotSent), context.CancellationToken);
                await _mediator.Send(new CreateFOBTStatus
                {
                    FOBT = fobt,
                    StatusCode = FOBTStatusCode.BillRequestNotSent
                }, context.CancellationToken);
            }
            else
            {
                _logger.LogInformation("Lab results not found, not able to publish BillRequestNotSent event");
            }
        }
    }

    private async Task SendBillingRequest(PdfDeliveredToClient pdfDeliveredToClientMessage, Fobt fobt, string billingProductCode, FOBTStatusCode statusCode,
        IMessageHandlerContext context)
    {
        var billingRequest = _mapper.Map<RCMRequestEvent>(fobt);
        billingRequest.ApplicationId = ApplicationConstants.BILLING_APPLICATION_ID;
        billingRequest.RcmProductCode = billingProductCode;
        billingRequest.StatusCode = statusCode.StatusCode;
        billingRequest.BillableDate = pdfDeliveredToClientMessage.DeliveryDateTime;
        billingRequest.SharedClientId = fobt.ClientId!.Value;
        billingRequest.CorrelationId = Guid.NewGuid().ToString();
        billingRequest.FOBT = fobt;
        billingRequest.BillingProductCode = billingProductCode;
        billingRequest.AdditionalDetails = new Dictionary<string, string>
        {
            { "BatchName", pdfDeliveredToClientMessage.BatchName },
            { "EvaluationId", billingRequest.EvaluationId.ToString() },
            { "appointmentId", fobt.AppointmentId.ToString() }
        };

        await context.Publish(billingRequest);
    }

    private async Task<bool> CanBillForResults(Fobt fobt, int evaluationId)
    {
        //Check for lab results received, apply state condition and make RCM call
        var validLabResultStatus = await _mediator.Send(new GetFobtStatusByStatusCodeAndEvaluationId
        {
            EvaluationId = evaluationId,
            FobtStatusCode = FOBTStatusCode.ValidLabResultsReceived
        });
        var isBillable = _billableRules.IsBillableForResults(new BillableRuleAnswers
        {
            Exam = fobt,
            IsValidLabResultsReceived = validLabResultStatus is not null
        }).IsMet;
        if (validLabResultStatus is not null && !isBillable)
        {
            _logger.LogInformation("Not billing for {BillingProductCode} although valid results were received, due to rules not being met",
                ResultsBillingProductCode);
        }

        return isBillable;
    }
}