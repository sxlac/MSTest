using MediatR;
using Microsoft.Extensions.Logging;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.Spirometry.Core.Queries
{
    /// <summary>
    /// Query to determine if an exam is payable, either by its EvaluationId or its performed exam results
    /// </summary>
    public class QueryPayable : IRequest<QueryPayableResult>
    {
        /// <summary>
        /// Identifier of the event that raised this request
        /// </summary>
        public Guid EventId { get; }
        /// <summary>
        /// Corresponding evaluation's identifier
        /// </summary>
        public long EvaluationId { get; }

        public SpirometryExamResult ExamResult { get; }

        /// <param name="eventId"></param>
        /// <param name="evaluationId"></param>
        /// <param name="examResult">Optional. Saves a database hit if you have the results already.</param>
        public QueryPayable(Guid eventId, long evaluationId, SpirometryExamResult examResult = null)
        {
            EventId = eventId;
            EvaluationId = evaluationId;
            ExamResult = examResult;
        }
    }

    public class QueryPayableResult
    {
        public bool IsPayable { get; }

        public QueryPayableResult(bool isPayable)
        {
            IsPayable = isPayable;
        }
    }

    public class QueryPayableHandler : IRequestHandler<QueryPayable, QueryPayableResult>
    {
        private readonly ILogger _logger;

        private readonly IMediator _mediator;

        public QueryPayableHandler(ILogger<QueryPayableHandler> logger,
            IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        public async Task<QueryPayableResult> Handle(QueryPayable request, CancellationToken cancellationToken)
        {
            // See https://wiki.signifyhealth.com/display/AncillarySvcs/Spirometry+Business+Rules

            var results = request.ExamResult ?? await _mediator.Send(new QueryExamResults(request.EvaluationId), cancellationToken);
            if (results != null) // Exam was performed and we have results
            {
                var isPayable = IsPayable(results, request);

                if (!isPayable)
                    _logger.LogInformation("EventId={EventId} is not payable, for EvaluationId={EvaluationId}", request.EventId, request.EvaluationId);

                return new QueryPayableResult(isPayable);
            }

            var notPerformed = await _mediator.Send(new QueryExamNotPerformed(request.EvaluationId), cancellationToken);
            if (notPerformed != null)
            {
                _logger.LogInformation("EventId={EventId} is not payable, exam was not performed, for EvaluationId={EvaluationId}", request.EventId, request.EvaluationId);
                return new QueryPayableResult(false);
            }

            // Should not normally get here, something must have gone wrong when inserting this Spirometry exam to db

            _logger.LogWarning("Unable to determine if EvaluationId={EvaluationId} is payable because we do not have results nor details that it was not performed, for EventId={EventId}",
                request.EvaluationId, request.EventId);

            // Raise for NSB retry
            throw new UnableToDeterminePayableException(request.EventId, request.EvaluationId);
        }

        private bool IsPayable(SpirometryExamResult result, QueryPayable request)
        {
            // See https://wiki.signifyhealth.com/display/AncillarySvcs/Spirometry+Business+Rules

            if (result.NormalityIndicatorId == NormalityIndicator.Normal.NormalityIndicatorId || result.NormalityIndicatorId == NormalityIndicator.Abnormal.NormalityIndicatorId)
                return true;

            if (result.NormalityIndicatorId != NormalityIndicator.Undetermined.NormalityIndicatorId)
                _logger.LogWarning("Invalid NormalityIndicatorId={NormalityIndicatorId}, setting payable to false, for EvaluationId={EvaluationId} with EventId={EventId}", result.NormalityIndicatorId, request.EvaluationId, request.EventId);

            return false;
        }
    }
}
