using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using EgfrNsbEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.Dps.Observability.Library.Services;
using Signify.eGFR.Core.BusinessRules;
using Signify.eGFR.Core.Commands;
using Signify.eGFR.Core.Constants;
using Signify.eGFR.Core.Data;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Events;
using Signify.eGFR.Core.Events.Akka;
using Signify.eGFR.Core.Exceptions;
using Signify.eGFR.Core.FeatureFlagging;
using Signify.eGFR.Core.Infrastructure;
using Signify.eGFR.Core.Models;
using Signify.eGFR.Core.Queries;
using NormalityIndicator = Signify.eGFR.Core.Configs.NormalityIndicator;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers;

public class LabResultReceivedHandler(
    ILogger<LabResultReceivedHandler> logger,
    IMediator mediator,
    ITransactionSupplier transactionSupplier,
    IPublishObservability publishObservability,
    IApplicationTime applicationTime,
    IMapper mapper,
    NormalityIndicator normalityIndicator,
    IFeatureFlags featureFlags,
    IBillableRules billableRules)
    : BaseNsbHandler(logger, mediator, transactionSupplier, publishObservability, applicationTime), IHandleMessages<KedEgfrLabResult>
{
    [Transaction]
    public async Task Handle(KedEgfrLabResult @event, IMessageHandlerContext context)
    {
        Logger.LogDebug("Start handling KedEgfrLabResult EvaluationId: {EvaluationId}", @event.EvaluationId);
        
        using var transaction = TransactionSupplier.BeginTransaction();
        
        Guid eventId = @event.EventId ?? Guid.NewGuid();
        
        // Find Exam by search DB for Evaluation ID
        var exam = await GetExam(@event, context.CancellationToken);

        //if exam does not exist publish new relic event and throw an exam not found exception
        if (exam == null)
        {
            Logger.LogError("Exam with EvaluationId: {EvaluationId} not found in DB", @event.EvaluationId);
            PublishObservabilityEvents(@event.EvaluationId, ApplicationTime.UtcNow(),
                Observability.LabResult.LabResultExamDoesNotExist,
                new Dictionary<string, object>
                {
                    { Observability.EventParams.EventId, eventId },
                    { Observability.EventParams.Result, @event.EgfrResult }
                }, true);
            throw new ExamNotFoundByEvaluationException(@event.EvaluationId, eventId);
        }

        // Ensure duplicate results do not get saved
        //search for LabResult
        var labResultExist = await Mediator.Send(new QueryLabResultByExamId(exam.ExamId), context.CancellationToken);

        if (labResultExist != null)
        {
            PublishObservabilityEvents(@event.EvaluationId, ApplicationTime.UtcNow(),
                Observability.LabResult.LabResultAlreadyExists,
                new Dictionary<string, object>
                {
                    { Observability.EventParams.EventId, eventId },
                    { Observability.EventParams.Result, @event.EgfrResult }
                }, true);
            Logger.LogWarning("KedLabResult for ExamId:{ExamID} EvaluationId: {EvaluationId} already exist in DB",
                exam.ExamId, @event.EvaluationId);
        }
        else
        {
            var labResult = mapper.Map<LabResult>(@event);
            labResult.ExamId = exam.ExamId;
            // Set normality indicator
            await SetNormalityIndicator(labResult);

            // Save labResult to DB LabResults table
            await Mediator.Send(new AddLabResult(labResult), context.CancellationToken);

            // Save a LabResultsReceived Status to the ExamStatus.
            var examStatus = new ExamStatus
            {
                ExamId = exam.ExamId,
                ExamStatusCodeId = ExamStatusCode.LabResultsReceived.StatusCodeId,
                CreatedDateTime = ApplicationTime.UtcNow(),
                StatusDateTime = exam.EvaluationReceivedDateTime
            };

            await Mediator.Send(new AddExamStatus(eventId, exam.EvaluationId, examStatus), context.CancellationToken);
            
            var resultsReceived = mapper.Map<ResultsReceived>(labResult);
            
            //check if the exam was performed before billing or publishing the results 
            var notPerformed = await Mediator.Send(new QueryExamNotPerformed(@event.EvaluationId), context.CancellationToken);
            if (notPerformed != null)
            {
                Logger.LogInformation("eGFR lab results were received for EvaluationId={EvaluationId} but exam was not performed, skipping billing and publishing results", @event.EvaluationId);
                PublishObservabilityEvents(@event.EvaluationId, ApplicationTime.UtcNow(),
                    Observability.LabResult.LabResultReceivedButExamNotPerformed,
                    new Dictionary<string, object>
                    {
                        { Observability.EventParams.EventId, eventId },
                        { Observability.EventParams.Determination, resultsReceived.Determination },
                        { Observability.EventParams.Result, @event.EgfrResult },
                        { Observability.EventParams.IsBillable, null }
                    }, true);
                await transaction.CommitAsync(context.CancellationToken);

                return;
            }
            
            // Retrieve billability value
            var billability = billableRules.IsBillable(new BillableRuleAnswers(exam.EvaluationId, eventId) { NormalityCode = resultsReceived.Determination });

            // Publish results Kafka message
            await Mediator.Send(new PublishResults(exam, resultsReceived, billability.IsMet), context.CancellationToken);

            //Check if pdf has been delivered and if so process billing
            var pdfEntity = (await Mediator.Send(new QueryPdfDeliveredToClient(exam.EvaluationId), context.CancellationToken)).Entity;

            if (pdfEntity != null)
            {
                if (featureFlags.EnableDirectBilling)
                {
                    await context.SendLocal(new ProcessBillingEvent(pdfEntity.EventId, pdfEntity.EvaluationId,
                        pdfEntity.PdfDeliveredToClientId, billability.IsMet, pdfEntity.CreatedDateTime, ProductCodes.EGfrRcmBillingResults));
                }
                else
                {
                    await context.SendLocal(new ProcessBillingEvent(pdfEntity.EventId, pdfEntity.EvaluationId,
                        pdfEntity.PdfDeliveredToClientId, billability.IsMet, pdfEntity.CreatedDateTime, ProductCodes.eGFR_RcmBilling));
                }
            }
            await ProcessPayment(exam, @event, eventId, context);
            PublishObservabilityEvents(@event.EvaluationId, ApplicationTime.UtcNow(),
                Observability.LabResult.LabResultReceived,
                new Dictionary<string, object>
                {
                    { Observability.EventParams.EventId, eventId },
                    { Observability.EventParams.Determination, resultsReceived.Determination },
                    { Observability.EventParams.Result, @event.EgfrResult },
                    { Observability.EventParams.IsBillable, billability.IsMet }
                }, true);
        }

        Logger.LogInformation("Finished handling KedEgfrLabResult Event EvaluationId={EvaluationId}",
            @event.EvaluationId);
        await transaction.CommitAsync(context.CancellationToken);
    }
    
    /// <summary>
    /// Handle Provider Payment
    /// </summary>
    /// <param name="exam"></param>
    /// <param name="egfrLabResult"></param>
    /// <param name="guid"></param>
    /// <param name="context"></param>
    private async Task ProcessPayment(Exam exam, KedEgfrLabResult egfrLabResult, Guid guid, IMessageHandlerContext context)
    {
        if (!featureFlags.EnableProviderPayCdi)
        {
            Logger.LogInformation("ProviderPay feature is NOT enabled for EvaluationId={EvaluationId}", exam.EvaluationId);
            return;
        }

        var cdiStatus = await Mediator.Send(new QueryPayableCdiStatus(exam.EvaluationId), context.CancellationToken);

        if (cdiStatus is not null)
        {
            await SendProviderPayRequest(exam, cdiStatus, egfrLabResult, guid, context);
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
    /// <param name="egfrLabResult"></param>
    /// <param name="guid"></param>
    /// <param name="context"></param>
    [Trace]
    private async Task SendProviderPayRequest(Exam exam, ExamStatus examStatus, KedEgfrLabResult egfrLabResult, Guid guid, IPipelineContext context)
    {
        var providerPayEventRequest = mapper.Map<ProviderPayRequest>(exam);
        providerPayEventRequest.EventId = guid;
        providerPayEventRequest.ParentEventDateTime = examStatus.StatusDateTime;
        providerPayEventRequest.ParentEventReceivedDateTime = egfrLabResult.ReceivedByEgfrDateTime;
        providerPayEventRequest.ParentEvent =
            examStatus.ExamStatusCodeId == ExamStatusCode.CdiPassedReceived.StatusCodeId ? nameof(CDIPassedEvent) : nameof(CDIFailedEvent);
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
        labResult.NormalityIndicatorId = labResult.EgfrResult switch
        {
            _ when labResult.EgfrResult >= normalityIndicator.Normal => Signify.eGFR.Core.Data.Entities.NormalityIndicator.Normal.NormalityIndicatorId,
            _ when labResult.EgfrResult < normalityIndicator.Normal && labResult.EgfrResult > 0 => Signify.eGFR.Core.Data.Entities.NormalityIndicator.Abnormal.NormalityIndicatorId
                ,
            _ => Signify.eGFR.Core.Data.Entities.NormalityIndicator.Undetermined.NormalityIndicatorId
        };

        return Task.CompletedTask;
    }

    private async Task<Exam> GetExam(KedEgfrLabResult labResult, CancellationToken token)
        => await Mediator.Send(new QueryExam(labResult.EvaluationId), token)
            .ConfigureAwait(false);
}