using System;
using System.Collections.Generic;
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

/// <summary>
/// NSB event handler for the <see cref="EgfrLabResult"/>
/// </summary>
public class QuestLabResultReceivedHandler(
    ILogger<QuestLabResultReceivedHandler> logger,
    IMediator mediator,
    ITransactionSupplier transactionSupplier,
    IPublishObservability publishObservability,
    IApplicationTime applicationTime,
    IFeatureFlags featureFlags,
    IMapper mapper,
    NormalityIndicator normalityIndicator,
    IBillableRules billableRules)
    : BaseNsbHandler(logger, mediator, transactionSupplier, publishObservability, applicationTime), IHandleMessages<EgfrLabResult>
{
    [Transaction]
    public async Task Handle(EgfrLabResult @event, IMessageHandlerContext context)
    {
        Logger.LogDebug("Start handling labResult CenseoId: {CenseoId}", @event.CenseoId);
        var labResult = mapper.Map<QuestLabResult>(@event);

        using var transaction = TransactionSupplier.BeginTransaction();
        var guid = Guid.NewGuid();
        // Find Exam by search DB for Evaluation ID, using CenseoId and DateOfService(CollectionDate)
        var exam = await GetExam(labResult);
        if (exam == null)
        {
            Logger.LogError("Exam with CenseoId: {CenseoId} and CollectionDate:{CollectionDate} not found in DB",
                labResult.CenseoId,
                labResult.CollectionDate);
            PublishObservabilityEvents(0, ApplicationTime.UtcNow(),
                Observability.LabResult.LabResultExamDoesNotExist,
                new Dictionary<string, object>
                {
                    { Observability.EventParams.CenseoId, @event.CenseoId },
                    { Observability.EventParams.CollectionDate, @event.CollectionDate }
                }, true);

            // Results may arrive before Exam is created - throw exception for NSB retry
            throw new ExamNotFoundException(labResult.CenseoId, labResult.CollectionDate);
        }

        // Ensure duplicate results do not get saved
        var labResultExist = await LabResultExistsInDatabase(labResult).ConfigureAwait(false);
        if (labResultExist != null)
        {
            PublishObservabilityEvents(exam.EvaluationId, ApplicationTime.UtcNow(),
                Observability.LabResult.LabResultAlreadyExists,
                new Dictionary<string, object>
                {
                    { Observability.EventParams.CenseoId, @event.CenseoId },
                    { Observability.EventParams.CollectionDate, @event.CollectionDate }
                }, true);
            Logger.LogWarning(
                "LabResult CenseoId: {CenseoId} and CollectionDate:{CollectionDate} already exist in DB",
                labResult.CenseoId, labResult.CollectionDate);
        }
        else
        {
            // Set normality indicator
            await SetNormalityIndicator(labResult);

            // Save labResult to DB LabResults table
            await Mediator.Send(new AddQuestLabResult(labResult), context.CancellationToken);

            // Save a LabResultsReceived Status to the ExamStatus.
            var examStatus = new ExamStatus
            {
                ExamId = exam.ExamId,
                ExamStatusCodeId = ExamStatusCode.LabResultsReceived.StatusCodeId,
                CreatedDateTime = ApplicationTime.UtcNow(),
                StatusDateTime = exam.EvaluationReceivedDateTime
            };
            await Mediator.Send(new AddExamStatus(guid, exam.EvaluationId, examStatus), context.CancellationToken);

            // Retrieve billability value
            var billability = billableRules.IsBillable(new BillableRuleAnswers(exam.EvaluationId, guid)
                { NormalityCode = labResult.NormalityCode });

            // Publish results Kafka message
            await Mediator.Send(new PublishResults(exam, mapper.Map<ResultsReceived>(labResult), billability.IsMet), context.CancellationToken);

            //Check if pdf has been delivered and if so process billing
            var pdfEntity = (await Mediator.Send(new QueryPdfDeliveredToClient(exam.EvaluationId), context.CancellationToken)).Entity;

            if (pdfEntity != null)
            {
                await context.SendLocal(new ProcessBillingEvent(pdfEntity.EventId, pdfEntity.EvaluationId,
                    pdfEntity.PdfDeliveredToClientId, billability.IsMet, pdfEntity.CreatedDateTime, ProductCodes.eGFR_RcmBilling));
            }

            await ProcessPayment(exam, @event, guid, context);

            PublishObservabilityEvents(exam.EvaluationId, ApplicationTime.UtcNow(),
                Observability.LabResult.LabResultReceived,
                new Dictionary<string, object>
                {
                    { Observability.EventParams.CenseoId, @event.CenseoId },
                    { Observability.EventParams.CollectionDate, @event.CollectionDate }
                }, true);
        }

        await transaction.CommitAsync(context.CancellationToken);
        Logger.LogInformation("Finished handling labResult CenseoId: {CenseoId} and CollectionDate:{CollectionDate}",
            labResult.CenseoId,
            labResult.CollectionDate);
    }

    /// <summary>
    /// Handle Provider Payment
    /// </summary>
    /// <param name="exam"></param>
    /// <param name="egfrLabResult"></param>
    /// <param name="guid"></param>
    /// <param name="context"></param>
    private async Task ProcessPayment(Exam exam, EgfrLabResult egfrLabResult, Guid guid, IMessageHandlerContext context)
    {
        if (!featureFlags.EnableProviderPayCdi)
        {
            Logger.LogInformation("ProviderPay feature is NOT enabled for EvaluationId={EvaluationId}",
                exam.EvaluationId);
            return;
        }

        var cdiStatus = await Mediator.Send(new QueryPayableCdiStatus(exam.EvaluationId), context.CancellationToken);
        if (cdiStatus is not null)
        {
            await SendProviderPayRequest(exam, cdiStatus, egfrLabResult, guid, context);
        }
        else
        {
            Logger.LogInformation(
                "A valid CDI event has not been received for EvaluationId={EvaluationId}. ProviderPay will not be triggered",
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
    private async Task SendProviderPayRequest(Exam exam, ExamStatus examStatus, EgfrLabResult egfrLabResult, Guid guid,
        IPipelineContext context)
    {
        var providerPayEventRequest = mapper.Map<ProviderPayRequest>(exam);
        providerPayEventRequest.EventId = guid;
        providerPayEventRequest.ParentEventDateTime = examStatus.StatusDateTime;
        providerPayEventRequest.ParentEventReceivedDateTime = egfrLabResult.ReceivedByEgfrDateTime;
        providerPayEventRequest.ParentEvent =
            examStatus.ExamStatusCodeId == ExamStatusCode.CdiPassedReceived.StatusCodeId
                ? nameof(CDIPassedEvent)
                : nameof(CDIFailedEvent);
        providerPayEventRequest.AdditionalDetails = new Dictionary<string, string>
        {
            { "EvaluationId", exam.EvaluationId.ToString() },
            { "AppointmentId", exam.AppointmentId.ToString() },
            { "ExamId", exam.ExamId.ToString() }
        };
        await context.SendLocal(providerPayEventRequest);
    }

    private async Task<QuestLabResult> LabResultExistsInDatabase(QuestLabResult questLabResult)
        => await Mediator.Send(new QueryQuestLabResult(questLabResult.CenseoId, questLabResult.CollectionDate))
            .ConfigureAwait(false);

    private async Task<Exam> GetExam(QuestLabResult questLabResult)
        => await Mediator.Send(new QueryExamByCenseoId(questLabResult.CenseoId, questLabResult.CollectionDate))
            .ConfigureAwait(false);

    private Task SetNormalityIndicator(QuestLabResult questLabResult)
    {
        questLabResult.Normality = questLabResult.eGFRResult switch
        {
            _ when questLabResult.eGFRResult >= normalityIndicator.Normal => Normality.Normal,
            _ when questLabResult.eGFRResult < normalityIndicator.Normal && questLabResult.eGFRResult > 0 => Normality
                .Abnormal,
            _ => Normality.Undetermined
        };
        questLabResult.NormalityCode = questLabResult.eGFRResult switch
        {
            _ when questLabResult.eGFRResult >= normalityIndicator.Normal => NormalityCodes.Normal,
            _ when questLabResult.eGFRResult < normalityIndicator.Normal && questLabResult.eGFRResult > 0 =>
                NormalityCodes.Abnormal,
            _ => NormalityCodes.Undetermined
        };

        return Task.CompletedTask;
    }
}