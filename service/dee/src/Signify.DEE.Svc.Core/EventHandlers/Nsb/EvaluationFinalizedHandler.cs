using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.DEE.Core.Messages.Queries;
using Signify.DEE.Messages.Status;
using Signify.DEE.Svc.Core.Commands;
using Signify.DEE.Svc.Core.Constants;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Events;
using Signify.DEE.Svc.Core.Infrastructure;
using Signify.DEE.Svc.Core.Messages.Commands;
using Signify.DEE.Svc.Core.Messages.Models;
using Signify.DEE.Svc.Core.Messages.Queries;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.EventHandlers.Nsb;

public class EvaluationFinalizedHandler(
    ILogger<EvaluationFinalizedHandler> logger,
    IMediator mediator,
    IMapper mapper,
    ITransactionSupplier transactionSupplier,
    IPublishObservability publishObservability,
    IApplicationTime applicationTime)
    : IHandleMessages<EvaluationFinalizedEvent>
{
    [Transaction]
    public async Task Handle(EvaluationFinalizedEvent message, IMessageHandlerContext context)
    {
        logger.LogDebug("Start Handle EvaluationFinalizedHandler, for EvaluationId={EvaluationId}", message.EvaluationId);

        using var transaction = transactionSupplier.BeginTransaction();

        // ANC-4563 - Tech debt, this should be refactored for explicitly processing DOS updates when finalized
        // more than once. This would save a query to the Evaluation API here.
        var answers = await GetAnswers(message);
        var response = await CreateExamModelAndSaveCreatedStatus(message, answers);

        if (response.IsNew) // else still commit the transaction, as a DOS update may have occurred
        {
            await SaveStatus(ExamStatusCode.ExamCreated, response.Exam.ExamId, message.CreatedDateTime.UtcDateTime);

            if (answers.Images.Count != 0)
            {
                await RecordPerformedExam(message, response.Exam, answers);
            }
            else
            {
                await RecordNotPerformedExam(message, response.Exam, answers, context);
            }
        }

        await transaction.CommitAsync(context.CancellationToken);
        logger.LogDebug("End Handle EvaluationFinalizedHandler, for EvaluationId={EvaluationId}", message.EvaluationId);
    }

    private Task<ExamAnswersModel> GetAnswers(EvaluationFinalizedEvent message)
    {
        // Get answers for this evaluation.
        var answerRequest = new GetEvalAnswers
        {
            EvaluationId = message.EvaluationId
        };
        return mediator.Send(answerRequest);
    }

    private Task<CreateExamRecordResponse> CreateExamModelAndSaveCreatedStatus(EvaluationFinalizedEvent message, ExamAnswersModel answers)
    {
        var examRequest = new CreateExamRecord
        {
            DateOfService = answers.DateOfService,
            EvaluationId = message.EvaluationId,
            MemberPlanId = answers.MemberPlanId,
            ProviderId = answers.ProviderId,
            RequestId = message.Id,
            ClientId = message.ClientId,
            AppointmentId = message.AppointmentId,
            State = answers.State,
            CreatedDateTime = message.CreatedDateTime.UtcDateTime,
            ReceivedDateTime = message.ReceivedDateTime,
            EvaluationObjective = message.Products.Exists(x => x.ProductCode.Equals("ASA", StringComparison.InvariantCultureIgnoreCase)) ?
                EvaluationObjective.Focused : EvaluationObjective.Comprehensive,
            RetinalImageTestingNotes = answers.RetinalImageTestingNotes,
            HasEnucleation = answers.HasEnucleation
        };
        PublishObservability(message, Observability.Evaluation.EvaluationReceivedEvent);

        // Create exam in database, if it does not already exist
        return mediator.Send(examRequest);
    }

    private async Task RecordPerformedExam(EvaluationFinalizedEvent message, ExamModel exam, ExamAnswersModel answers)
    {
        await SaveStatus(ExamStatusCode.DEEImagesFound, exam.ExamId, message.CreatedDateTime.UtcDateTime);
        await SaveStatus(ExamStatusCode.Performed, exam.ExamId, message.CreatedDateTime.UtcDateTime);
        await PublishPerformedEvent(message, exam);

        await mediator.Send(new CreateIrisOrder() { Exam = exam, ExamAnswers = answers });

        await mediator.Send(new CreateStatus(exam.ExamId, ExamStatusCode.IrisOrderSubmitted.Name, applicationTime.UtcNow()));

        Dictionary<string, string> imageIdToRawImageMap = new();
        foreach (var img in answers.Images)
        {
            var savedImage = await mediator.Send(new CreateExamImage() { Exam = exam });
            imageIdToRawImageMap.Add(savedImage.ImageLocalId, img);
        }

        // 2-13-2024 Iris' image ingestion was failing due to processing images before orders.
        // While they have introduced a fix, we are adding this delay similar to the API process,
        // maybe temporarily, to help mitigate this issue.

        await Task.Delay(2000);

        await mediator.Send(new UploadIrisImages()
        {
            Exam = exam,
            ExamAnswers = answers,
            ImageIdToRawImageMap = imageIdToRawImageMap
        });

        await mediator.Send(new CreateStatus(exam.ExamId, ExamStatusCode.IrisImagesSubmitted.Name, applicationTime.UtcNow()));

        PublishObservability(message, Observability.Evaluation.EvaluationPerformedEvent);
    }

    private async Task RecordNotPerformedExam(EvaluationFinalizedEvent message, ExamModel exam, ExamAnswersModel answers, IMessageHandlerContext context)
    {
        // Record NoImagesTaken and NotPerformed.
        await SaveStatus(ExamStatusCode.NoDEEImagesTaken, exam.ExamId, message.CreatedDateTime.UtcDateTime);
        await SaveStatus(ExamStatusCode.NotPerformed, exam.ExamId, message.CreatedDateTime.UtcDateTime);
        logger.LogInformation("No DEE images found in evaluation answers for EvaluationId={EvaluationId}, treating as Not Performed", message.EvaluationId);

        var notPerformedModel = await mediator.Send(new GetNotPerformedModel { EvaluationId = message.EvaluationId, Answers = answers.Answers }, context.CancellationToken);

        //If not performed Reason found then add it to the DB.
        if (notPerformedModel != null)
        {
            await mediator.Send(new AddDeeNotPerformed { NotPerformedModel = notPerformedModel, ExamModel = exam }, context.CancellationToken);
            var notPerformed = mapper.Map<NotPerformed>(message);
            mapper.Map(notPerformedModel, notPerformed);
            mapper.Map(exam, notPerformed);
            await mediator.Send(new PublishStatusUpdate(notPerformed), context.CancellationToken);
        }
        else
        {
            logger.LogWarning("Evaluation was Not Performed, but no not performed reason was found in evaluation answers, for EvaluationId={EvaluationId}", message.EvaluationId);
        }

        // Release the cdi hold for DEE if it exists.        
        var hold = await mediator.Send(new GetHold() { EvaluationId = message.EvaluationId }, context.CancellationToken);
        if (hold is not null)
        {
            await context.SendLocal(new ReleaseHold(hold));
        }

        PublishObservability(message, Observability.Evaluation.EvaluationNotPerformedEvent);
    }

    private async Task SaveStatus(ExamStatusCode statusCode, int examId, DateTime utcDateTime)
    {
        await mediator.Send(new CreateStatus(examId, statusCode.Name, utcDateTime));
    }

    private async Task PublishPerformedEvent(EvaluationFinalizedEvent message, ExamModel exam)
    {
        var status = mapper.Map<Performed>(message);
        mapper.Map(exam, status);
        await mediator.Send(new PublishStatusUpdate(status));
    }

    private void PublishObservability(EvaluationFinalizedEvent message, string eventType)
    {
        var observabilityEvent = new ObservabilityEvent
        {
            EvaluationId = message.EvaluationId,
            EventType = eventType,
            EventValue = new Dictionary<string, object>
            {
                { Observability.EventParams.EvaluationId, message.EvaluationId },
                { Observability.EventParams.CreatedDateTime, message.CreatedDateTime.ToUnixTimeSeconds() }
            }
        };

        publishObservability.RegisterEvent(observabilityEvent, true);
    }
}