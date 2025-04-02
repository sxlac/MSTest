using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Data.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.Spirometry.Core.Commands
{
    /// <summary>
    /// Command to add a <see cref="ExamStatus"/> record to database
    /// </summary>
    public class AddExamStatus : IRequest<AddExamStatusResponse>
    {
        public Guid EventId { get; }
        public long EvaluationId { get; }
        public ExamStatus Status { get; }
        public bool AlwaysAddStatus { get; set; }

        public AddExamStatus(Guid eventId, long evaluationId, ExamStatus status, bool alwaysAddStatus = false)
        {
            EventId = eventId;
            EvaluationId = evaluationId;
            Status = status;
            AlwaysAddStatus = alwaysAddStatus;
        }
    }

    /// <summary>
    /// Response for <see cref="AddExamStatus"/> command
    /// </summary>
    public class AddExamStatusResponse
    {
        public ExamStatus Status { get; }

        /// <summary>
        /// Whether or not this status was just inserted, or if it already existed in the database
        /// </summary>
        public bool IsNew { get; }

        public AddExamStatusResponse(ExamStatus status, bool isNew)
        {
            Status = status;
            IsNew = isNew;
        }
    }

    public class AddExamStatusHandler : IRequestHandler<AddExamStatus, AddExamStatusResponse>
    {
        private readonly ILogger _logger;
        private readonly SpirometryDataContext _dataContext;

        public AddExamStatusHandler(ILogger<AddExamStatusHandler> logger, SpirometryDataContext dataContext)
        {
            _logger = logger;
            _dataContext = dataContext;
        }

        public async Task<AddExamStatusResponse> Handle(AddExamStatus request, CancellationToken cancellationToken)
        {
            if (!request.AlwaysAddStatus)
            {
                var entity = await _dataContext.ExamStatuses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s =>
                            s.SpirometryExamId == request.Status.SpirometryExamId && s.StatusCodeId == request.Status.StatusCodeId,
                        cancellationToken);

                if (entity != null)
                {
                    _logger.LogInformation(
                        "ExamStatus already exists for EvaluationId={EvaluationId} with StatusCodeId={StatusCodeId}; returning existing record",
                        request.EvaluationId, request.Status.StatusCodeId);

                    return new AddExamStatusResponse(entity, false);
                }
            }

            var newEntity = (await _dataContext.ExamStatuses.AddAsync(request.Status, cancellationToken)).Entity;

            await _dataContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully inserted a new ExamStatus record for EvaluationId={EvaluationId}, new ExamStatusId={ExamStatusId}",
                request.EvaluationId, newEntity.ExamStatusId);

            return new AddExamStatusResponse(newEntity, true);
        }
    }
}