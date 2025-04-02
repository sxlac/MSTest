using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Signify.uACR.Core.Constants;
using Signify.uACR.Core.Data;
using Signify.uACR.Core.Data.Entities;
using UacrNsbEvents;

namespace Signify.uACR.Core.Commands;

/// <summary>
/// Command to update an existing entity in the Exam table from a given <see cref="EvalReceived"/> event
/// </summary>
public class UpdateExam(EvalReceived eventData) : IRequest<Exam>
{
    public EvalReceived EventData { get; } = eventData;
}

public class UpdateExamHandler(
    ILogger<UpdateExamHandler> logger,
    DataContext dataContext,
    IPublishObservability publishObservability)
    : IRequestHandler<UpdateExam, Exam>
{
    public async Task<Exam>
        Handle(UpdateExam request,
            CancellationToken cancellationToken) // Remove now or leave? There is a future story for this?
    {
        if (!request.EventData.DateOfService.HasValue)
        {
            logger.LogInformation(
                "EvaluationId={EvaluationId} - Nothing to update for this exam because DateOfService is null",
                request.EventData.EvaluationId);
            return null;
        }

        var entity = await dataContext.Exams
            .FirstAsync(each => each.EvaluationId == request.EventData.EvaluationId, cancellationToken)
            .ConfigureAwait(false);

        if (entity.DateOfService == request.EventData.DateOfService)
        {
            logger.LogInformation(
                "EvaluationId={EvaluationId} - Nothing to update for this exam because DateOfService has not changed: {DateOfService}",
                entity.EvaluationId, entity.DateOfService);

            return null;
        }

        var priorDateOfService = entity.DateOfService;
        entity.DateOfService = request.EventData.DateOfService;

        await dataContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "EvaluationId={EvaluationId} - DOS updated to {UpdatedDateOfService}; previous DOS {PriorDateOfService}",
            entity.EvaluationId, entity.DateOfService, priorDateOfService);

        PublishObservability(entity, Observability.Evaluation.EvaluationDosUpdatedEvent);
        return entity;
    }

    private void PublishObservability(Exam exam, string eventType)
    {
        var observabilityDosUpdatedEvent = new ObservabilityEvent
        {
            EvaluationId = exam.EvaluationId,
            EventType = eventType,
            EventValue = new Dictionary<string, object>
            {
                {Observability.EventParams.EvaluationId, exam.EvaluationId},
                {Observability.EventParams.CreatedDateTime, exam.CreatedDateTime.ToUnixTimeSeconds()}
            }
        };

        publishObservability.RegisterEvent(observabilityDosUpdatedEvent, true);
    }
}