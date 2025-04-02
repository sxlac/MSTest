using System.Collections.Generic;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.eGFR.Core.Data;
using Signify.eGFR.Core.Data.Entities;
using EgfrNsbEvents;
using System.Threading;
using System.Threading.Tasks;
using Signify.eGFR.Core.Constants;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;

namespace Signify.eGFR.Core.Commands;

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
    private readonly ILogger _logger = logger;

    public async Task<Exam> Handle(UpdateExam request, CancellationToken cancellationToken) // Remove now or leave? There is a future story for this?
    {
        if (!request.EventData.DateOfService.HasValue)
        {
            _logger.LogInformation("EvaluationId={EvaluationId} - Nothing to update for this exam because DateOfService is null",
                request.EventData.EvaluationId);
            return null;
        }

        var entity = await dataContext.Exams
            .FirstAsync(each => each.EvaluationId == request.EventData.EvaluationId, cancellationToken)
            .ConfigureAwait(false);

        if (entity.DateOfService == request.EventData.DateOfService)
        {
            _logger.LogInformation("EvaluationId={EvaluationId} - Nothing to update for this exam because DateOfService has not changed: {DateOfService}",
                entity.EvaluationId, entity.DateOfService);

            return null;
        }

        var priorDateOfService = entity.DateOfService;
        entity.DateOfService = request.EventData.DateOfService;

        await dataContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("EvaluationId={EvaluationId} - DOS updated to {UpdatedDateOfService}; previous DOS {PriorDateOfService}",
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
                { Observability.EventParams.EvaluationId, exam.EvaluationId },
                { Observability.EventParams.CreatedDateTime, exam.CreatedDateTime.ToUnixTimeSeconds() }
            }
        };

        publishObservability.RegisterEvent(observabilityDosUpdatedEvent, true);
    }
}