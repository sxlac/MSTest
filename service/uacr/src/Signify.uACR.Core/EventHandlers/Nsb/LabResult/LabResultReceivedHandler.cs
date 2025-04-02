using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.uACR.Core.BusinessRules;
using Signify.uACR.Core.Commands;
using Signify.uACR.Core.Configs;
using Signify.uACR.Core.Constants;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Data;
using Signify.uACR.Core.Events.Akka;
using Signify.uACR.Core.Events;
using Signify.uACR.Core.Exceptions;
using Signify.uACR.Core.FeatureFlagging;
using Signify.uACR.Core.Infrastructure;
using Signify.uACR.Core.Models;
using Signify.uACR.Core.Queries;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using NsbEventHandlers;
using Signify.Dps.Observability.Library.Services;
using UacrNsbEvents;

namespace Signify.uACR.Core.EventHandlers.Nsb;

public class LabResultReceivedHandler(
    ILogger<LabResultReceivedHandler> logger,
    IMapper mapper,
    IMediator mediator,
    IApplicationTime applicationTime,
    ITransactionSupplier transactionSupplier,
    IPublishObservability publishObservability,
    NormalityIndicator normalityIndicator,
    IFeatureFlags featureFlags,
    IBillableRules billableRules)
    : BaseNsbHandler(logger, mediator, transactionSupplier, publishObservability, applicationTime), IHandleMessages<KedUacrLabResult>
{
    [Transaction]
    public async Task Handle(KedUacrLabResult @event, IMessageHandlerContext context)
    {
        Logger.LogDebug("Start handling labResult EvaluationId: {EvaluationId}", @event.EvaluationId);
       
        using var transaction = TransactionSupplier.BeginTransaction();
        
        var exam = await Mediator.Send(new QueryExamByEvaluation{ EvaluationId = @event.EvaluationId }, context.CancellationToken).ConfigureAwait(false);
        
        Guid eventId = @event.EventId ?? Guid.NewGuid();
        
        //if exam does not exist publish new relic event and throw an exam not found exception
        if (exam == null)
        {
            Logger.LogError("Exam with EvaluationId: {EvaluationId} not found in DB", @event.EvaluationId);
            
            //check if both ked product codes are attached to the evaluation 
            var productCodes = await Mediator.Send(new QueryEvaluationProductCodes(@event.EvaluationId), context.CancellationToken).ConfigureAwait(false);
            
            if (!productCodes.Contains(ProductCodes.uACR)  || !productCodes.Contains(ProductCodes.eGFR))
            {
                //set the billability and raise custom exception and new relic event
                var labResult = mapper.Map<LabResult>(@event);
                await SetNormalityIndicator(labResult);
                var billability = billableRules.IsBillable(new BillableRuleAnswers(@event.EvaluationId, Guid.NewGuid())
                    { Result = labResult });

                PublishObservabilityEvents(@event.EvaluationId, ApplicationTime.UtcNow(),
                    Observability.LabResult.LabResultReceivedButProductCodeMissing,
                    new Dictionary<string, object>()
                    {
                        { Observability.EventParams.EventId, eventId },
                        { Observability.EventParams.IsBillable, billability.IsMet }
                    }, true);
                
                throw new KedProductNotFoundException(@event.EvaluationId, @event.UacrResult, billability.IsMet);
            }
            
            //publish new relic event
            PublishObservabilityEvents(@event.EvaluationId, ApplicationTime.UtcNow(),
                Observability.LabResult.LabResultExamDoesNotExist,
                new Dictionary<string, object>()
                {
                    { Observability.EventParams.EventId, eventId },
                    {Observability.EventParams.Result, @event.UacrResult}
                }, true);
            
            throw new ExamNotFoundException(@event.EvaluationId, eventId);
        }
        
        // Ensure duplicate results do not get saved
        var labResultExist = await Mediator.Send(new QueryLabResultByEvaluationId(@event.EvaluationId), context.CancellationToken).ConfigureAwait(false);

        if (labResultExist != null)
        {
            //publish new relic event
            PublishObservabilityEvents(@event.EvaluationId, ApplicationTime.UtcNow(),
                Observability.LabResult.LabResultAlreadyExists,
                new Dictionary<string, object>()
                {
                    { Observability.EventParams.EventId, eventId },
                    { Observability.EventParams.Result, @event.UacrResult }
                }, true);
            Logger.LogWarning("LabResult for EvaluationId: {EvaluationId} already exist in DB", @event.EvaluationId);
        }
        else
        {
            var labResult = mapper.Map<LabResult>(@event);
            
            // Set normality indicator
            await SetNormalityIndicator(labResult);
            
            // Save labResult to DB LabResults table
            await Mediator.Send(new AddLabResult(labResult), context.CancellationToken);
            
            // Save a LabResultsReceived Status to the ExamStatus.
            var examStatus = new ExamStatus
            {
                ExamId = exam.ExamId,
                ExamStatusCodeId = ExamStatusCode.LabResultsReceived.ExamStatusCodeId,
                CreatedDateTime = ApplicationTime.UtcNow(),
                StatusDateTime = exam.EvaluationReceivedDateTime
            };
            
            await Mediator.Send(new AddExamStatus(eventId, exam.EvaluationId, examStatus), context.CancellationToken);
            
            //check if the exam was performed before billing or publishing the results 
            var notPerformed = await Mediator.Send(new QueryExamNotPerformed(@event.EvaluationId), context.CancellationToken);
            if (notPerformed != null)
            {
                Logger.LogInformation("uACR lab results were received for EvaluationId={EvaluationId} but exam was not performed, skipping billing and publishing results.", @event.EvaluationId);
            
                PublishObservabilityEvents(@event.EvaluationId, ApplicationTime.UtcNow(),
                    Observability.LabResult.LabResultReceivedButExamNotPerformed,
                    new Dictionary<string, object>()
                    {
                        { Observability.EventParams.EventId, eventId },
                        { Observability.EventParams.Result, @event.UacrResult },
                        { Observability.EventParams.Determination, labResult.NormalityCode },
                        { Observability.EventParams.IsBillable, null }
                    }, true);
                await transaction.CommitAsync(context.CancellationToken);
                return;
            }
            
            var billability = billableRules.IsBillable(new BillableRuleAnswers(exam.EvaluationId, eventId)
                { Result = labResult });
            
            // Publish results Kafka message
            await Mediator.Send(new PublishResults(exam, labResult, billability.IsMet), context.CancellationToken);
            
            if (featureFlags.EnableBilling)
            {
                //Check if pdf has been delivered and if so process billing
                var pdfEntity = (await Mediator.Send(new QueryPdfDeliveredToClient(exam.EvaluationId), context.CancellationToken)).Entity;
                if (pdfEntity != null)
                {
                    if (featureFlags.EnableDirectBilling)
                    {
                        await context.SendLocal(new ProcessBillingEvent(pdfEntity.EventId, pdfEntity.EvaluationId,
                            pdfEntity.PdfDeliveredToClientId, billability.IsMet, pdfEntity.CreatedDateTime, ProductCodes.UAcrRcmBillingResults));
                    }
                    else
                    {
                        await context.SendLocal(new ProcessBillingEvent(pdfEntity.EventId, pdfEntity.EvaluationId,
                            pdfEntity.PdfDeliveredToClientId, billability.IsMet, pdfEntity.CreatedDateTime, ProductCodes.uACR_RcmBilling));
                    }
                }
            }
            
            await ProcessPayment(exam, @event, eventId, context);
            //publish new relic event
            PublishObservabilityEvents(@event.EvaluationId, ApplicationTime.UtcNow(),
                Observability.LabResult.LabResultReceived, new Dictionary<string, object>()
                {
                    { Observability.EventParams.EventId, eventId },
                    { Observability.EventParams.Result, labResult.UacrResult },
                    { Observability.EventParams.IsBillable, billability.IsMet },
                    { Observability.EventParams.Determination, labResult.NormalityCode }
                }, true);
        }
        
        Logger.LogInformation("Finished handling LabResultReceived Event EvaluationId={EvaluationId}", @event.EvaluationId);
        await transaction.CommitAsync(context.CancellationToken);
    }
    
    /// <summary>
    /// Handle Provider Payment
    /// </summary>
    /// <param name="exam"></param>
    /// <param name="kedUacrLabResult"></param>
    /// <param name="guid"></param>
    /// <param name="context"></param>
    private async Task ProcessPayment(Exam exam, KedUacrLabResult kedUacrLabResult, Guid guid, IPipelineContext context)
    {
        if (!featureFlags.EnableProviderPayCdi)
        {
            Logger.LogInformation("ProviderPay feature is NOT enabled for EvaluationId={EvaluationId}", exam.EvaluationId);
            return;
        }

        var examStatus = await Mediator.Send(new QueryPayableCdiStatus { EvaluationId = exam.EvaluationId }, context.CancellationToken);

        if (examStatus is not null)
        {
            await SendProviderPayRequest(exam, examStatus, kedUacrLabResult, guid, context);
        }
        else
        {
            Logger.LogInformation("A valid CDI event has not been received for EvaluationId={EvaluationId}. ProviderPay will not be triggered",
                exam.EvaluationId);
        }
    }
    
    /// <summary>
    /// Send NSB event to handle database write and kafka event
    /// </summary>
    /// <param name="exam"></param>
    /// <param name="examStatus"></param>
    /// <param name="kedUacrLabResult"></param>
    /// <param name="guid"></param>
    /// <param name="context"></param>
    [Trace]
    private async Task SendProviderPayRequest(Exam exam, ExamStatus examStatus, KedUacrLabResult kedUacrLabResult, Guid guid, IPipelineContext context)
    {
        var providerPayEventRequest = mapper.Map<ProviderPayRequest>(exam);
        providerPayEventRequest.EventId = guid;
        providerPayEventRequest.ParentEventDateTime = examStatus.StatusDateTime;
        providerPayEventRequest.ParentEventReceivedDateTime = kedUacrLabResult.DateLabReceived;
        providerPayEventRequest.ParentEvent =
            examStatus.ExamStatusCodeId == ExamStatusCode.CdiPassedReceived.ExamStatusCodeId ? nameof(CDIPassedEvent) : nameof(CDIFailedEvent);
        providerPayEventRequest.AdditionalDetails = new Dictionary<string, string>
        {
            { "EvaluationId", exam.EvaluationId.ToString() },
            { "AppointmentId", exam.AppointmentId.ToString() },
            { "ExamId", exam.ExamId.ToString() }
        };
        await context.SendLocal(providerPayEventRequest);
    }
    
    private Task SetNormalityIndicator(LabResult labResult)
    {
        labResult.Normality = labResult.UacrResult switch
        {
            _ when labResult.UacrResult >= normalityIndicator.Normal => Normality.Abnormal,
            _ when labResult.UacrResult < normalityIndicator.Normal && labResult.UacrResult > 0 => Normality.Normal,
            _ => Normality.Undetermined
        };
        labResult.NormalityCode = labResult.UacrResult switch
        {
            _ when labResult.UacrResult >= normalityIndicator.Normal => NormalityCodes.Abnormal,
            _ when labResult.UacrResult < normalityIndicator.Normal && labResult.UacrResult > 0 => NormalityCodes.Normal,
            _ => NormalityCodes.Undetermined
        };

        return Task.CompletedTask;
    }
}