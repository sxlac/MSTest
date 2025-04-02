using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Newtonsoft.Json;
using NServiceBus;
using Signify.DEE.Svc.Core.ApiClient;
using Signify.DEE.Svc.Core.ApiClient.Requests;
using Signify.DEE.Svc.Core.Commands;
using Signify.DEE.Svc.Core.Constants;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Events;
using Signify.DEE.Svc.Core.Exceptions;
using Signify.DEE.Svc.Core.Infrastructure;
using Signify.DEE.Svc.Core.Messages.Commands;
using Signify.DEE.Svc.Core.Messages.Models;
using Signify.DEE.Svc.Core.Messages.Queries;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using BillRequestSent = Signify.DEE.Messages.Status.BillRequestSent;

namespace Signify.DEE.Svc.Core.EventHandlers;

public class RcmInvoiceHandler(
    ILogger<RcmInvoiceHandler> logger,
    IRCMApi rcmApi,
    IMapper mapper,
    IMediator mediator,
    IApplicationTime applicationTime,
    ITransactionSupplier transactionSupplier,
    IPublishObservability publishObservability)
    : IHandleMessages<RCMBillingRequestEvent>
{
    [Transaction]
    public async Task Handle(RCMBillingRequestEvent message, IMessageHandlerContext context)
    {
        logger.LogInformation("Start Handle RCMInvoiceHandler, EvaluationID: {EvaluationId}, ExamId: {ExamId}", message.EvaluationId, message.ExamId);

        var exam = await mediator.Send(new GetExamRecord { EvaluationId = message.EvaluationId }, context.CancellationToken).ConfigureAwait(false);

        var rcmBilling = mapper.Map<RCMBilling>(message);

        mapper.Map(exam, rcmBilling);

        logger.LogInformation("Sending DEE rcmBilling EvaluationId : {EvaluationId}, CorrelationId : {CorrelationId}", message.EvaluationId, rcmBilling.CorrelationId);
        //for logging the bill details --End

        var rcmRs = await rcmApi.SendRCMRequestForBilling(rcmBilling);
        RegisterObservability(message, Observability.ProviderPay.RcmBillingApiStatusCodeEvent, Observability.EventParams.StatusCode,
            rcmRs?.StatusCode, true);
        RegisterObservability(message, Observability.ProviderPay.ProviderPayOrBillingEvent, Observability.EventParams.Type,
            Observability.EventParams.TypeRcmBilling);
        if (rcmRs.IsSuccessStatusCode || rcmRs.StatusCode == HttpStatusCode.MovedPermanently)
        {
            //When Response is 202 : cmRs.Content will be not  null and Error will have null value. 
            //When Response is 301(MovedPermanently) : cmRs.Content will be null and Error will have  value. 
            var billId = rcmRs.Content == null && rcmRs.Error != null ? JsonConvert.DeserializeObject(rcmRs.Error.Content).ToString() : rcmRs.Content.ToString();

            PublishSuccessObservabilityEvent(message, Observability.RcmBilling.BillRequestRaisedEvent, billId);

            if (!string.IsNullOrWhiteSpace(billId)) //ignore if rcm is not enabled yet
            {
                var responseReceivedTime = applicationTime.UtcNow();

                using var transaction = transactionSupplier.BeginTransaction();

                await mediator.Send(new CreateRcmBilling { RcmBilling = new DEEBilling { BillId = billId, ExamId = message.ExamId, CreatedDateTime = responseReceivedTime, RcmProductCode = message.RcmProductCode } }, context.CancellationToken);
                await mediator.Send(new CreateStatus(message.ExamId, ExamStatusCode.SentToBilling.Name, responseReceivedTime), context.CancellationToken);

                await PublishStatusUpdate(message, billId, responseReceivedTime, exam);
                await transaction.CommitAsync(context.CancellationToken);
                publishObservability.Commit();
            }
            else
            {
                logger.LogInformation("DEE rcmBilling success but ignored EvaluationId : {EvaluationId}, CorrelationId : {CorrelationId}", message.EvaluationId, rcmBilling.CorrelationId);
                throw new RcmBillingException($"DEE rcmBilling success but no bill Id received for EvaluationId {message.EvaluationId}");
            }
        }
        else
        {
            if (rcmRs.ReasonPhrase == "Conflict")
            {
                logger.LogError(
                    "DEE rcmBilling conflict EvaluationId : {EvaluationId}, CorrelationId : {CorrelationId}",
                    message.EvaluationId,
                    rcmBilling
                        .CorrelationId); //Only log the conflict and donot retry, as per discussion with T-checks
            }
            else
            {
                PublishFailedObservabilityEvent(message, Observability.RcmBilling.BillRequestFailedEvent);
                throw new RcmBillingException($"There is error in RCM | Detail: {(rcmRs.Content != null && rcmRs.Content == null ? "" : JsonConvert.SerializeObject(rcmRs.Content))}, Error: {(rcmRs.Error == null ? "" : JsonConvert.SerializeObject(rcmRs.Error))}, ReasonPhrase: {rcmRs.ReasonPhrase}");
            }
        }
    }

    private async Task PublishStatusUpdate(RCMBillingRequestEvent message, string billId, DateTimeOffset now, ExamModel exam)
    {
        var billRequestSent = mapper.Map<BillRequestSent>(message);
        billRequestSent.BillId = billId;
        billRequestSent.ReceivedDate = now;
        billRequestSent.CreateDate = exam.CreatedDateTime;
        billRequestSent.MemberPlanId = exam.MemberPlanId;
        await mediator.Send(new PublishStatusUpdate(billRequestSent));
    }

    #region Observability

    private void RegisterObservability(RCMBillingRequestEvent message, string eventType, string eventParam, object eventParamValue, bool sendImmediate = false)
    {
        var observabilityEvent = new ObservabilityEvent
        {
            EvaluationId = message.EvaluationId,
            EventId = Guid.TryParse(message.CorrelationId, out var result) ? result : (Guid?)null,
            EventType = eventType,
            EventValue = new Dictionary<string, object>
            {
                { Observability.EventParams.EvaluationId, message.EvaluationId },
                { eventParam, eventParamValue }
            }
        };

        publishObservability.RegisterEvent(observabilityEvent, sendImmediate);
    }
    private void PublishFailedObservabilityEvent(RCMBillingRequestEvent request, string eventType)
    {
        var observabilityBillRequestFailedEvent = new ObservabilityEvent
        {
            EvaluationId = request.EvaluationId,
            EventType = eventType,
            EventValue = new Dictionary<string, object>
            {
                {Observability.EventParams.EvaluationId, request.EvaluationId},
                {Observability.EventParams.CreatedDateTime, ((DateTimeOffset)request.BillableDate).ToUnixTimeSeconds()}
            }
        };

        publishObservability.RegisterEvent(observabilityBillRequestFailedEvent, true);
    }
    private void PublishSuccessObservabilityEvent(RCMBillingRequestEvent request, string eventType, string billId)
    {
        var observabilityBillRequestRaisedEvent = new ObservabilityEvent
        {
            EvaluationId = request.EvaluationId,
            EventType = eventType,
            EventValue = new Dictionary<string, object>
            {
                {Observability.EventParams.EvaluationId, request.EvaluationId},
                {Observability.EventParams.CreatedDateTime, ((DateTimeOffset)request.BillableDate).ToUnixTimeSeconds()},
                {Observability.EventParams.BillId, billId}
            }
        };

        publishObservability.RegisterEvent(observabilityBillRequestRaisedEvent, true);
    }

    #endregion
}