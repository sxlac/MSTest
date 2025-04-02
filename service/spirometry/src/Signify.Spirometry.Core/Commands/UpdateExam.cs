using System;
using System.Collections.Generic;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Data.Entities;
using SpiroNsbEvents;
using System.Threading;
using System.Threading.Tasks;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Signify.Spirometry.Core.Constants;

namespace Signify.Spirometry.Core.Commands
{
    /// <summary>
    /// Command to update an existing entity in the SpirometryExam table from a given <see cref="EvalReceived"/> event
    /// </summary>
    public class UpdateExam : IRequest<SpirometryExam>
    {
        public EvalReceived EventData { get; }

        public UpdateExam(EvalReceived eventData)
        {
            EventData = eventData;
        }
    }

    public class UpdateExamHandler : IRequestHandler<UpdateExam, SpirometryExam>
    {
        private readonly ILogger _logger;
        private readonly SpirometryDataContext _spirometryDataContext;
        private readonly IPublishObservability _publishObservability;
        
        public UpdateExamHandler(ILogger<UpdateExamHandler> logger,
            SpirometryDataContext spirometryDataContext,
            IPublishObservability publishObservability)
        {
            _logger = logger;
            _spirometryDataContext = spirometryDataContext;
            _publishObservability = publishObservability;
        }

        public async Task<SpirometryExam> Handle(UpdateExam request, CancellationToken cancellationToken)
        {
            if (!request.EventData.DateOfService.HasValue)
            {
                _logger.LogInformation("EvaluationId={EvaluationId} - Nothing to update for this exam because DateOfService is null",
                    request.EventData.EvaluationId);
                return null;
            }

            var entity = await _spirometryDataContext.SpirometryExams
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

            await _spirometryDataContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("EvaluationId={EvaluationId} - DOS updated to {UpdatedDateOfService}; previous DOS {PriorDateOfService}",
                entity.EvaluationId, entity.DateOfService, priorDateOfService);

            PublishObservability(entity, Observability.Evaluation.EvaluationDosUpdatedEvent);
            return entity;
        }
        private void PublishObservability(SpirometryExam exam, string eventType)
        {
            var observabilityDosUpdatedEvent = new ObservabilityEvent
            {
                EvaluationId = exam.EvaluationId,
                EventType = eventType,
                EventValue = new Dictionary<string, object>
                {
                    { Observability.EventParams.EvaluationId, exam.EvaluationId },
                    { Observability.EventParams.CreatedDateTime, ((DateTimeOffset)exam.CreatedDateTime).ToUnixTimeSeconds() }
                }
            };

            _publishObservability.RegisterEvent(observabilityDosUpdatedEvent, true);
        }
    }
}
