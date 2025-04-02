using MediatR;
using Microsoft.Extensions.Logging;
using Signify.eGFR.Core.Data.Entities;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Signify.eGFR.Core.Constants;

namespace Signify.eGFR.Core.Queries;

/// <summary>
/// Query to determine if an exam is billable, either by its EvaluationId or its performed exam results
/// </summary>
[ExcludeFromCodeCoverage]
public class QueryBillability : IRequest<QueryBillabilityResult>
{
    /// <summary>
    /// Identifier of the event that raised this request
    /// </summary>
    public Guid EventId { get; }
    /// <summary>
    /// Corresponding evaluation's identifier
    /// </summary>
    public long EvaluationId { get; }

    public QuestLabResult QuestLabResult { get; }

    /// <param name="eventId"></param>
    /// <param name="evaluationId"></param>
    /// <param name="questLabResult">Optional. Saves a database hit if you have the results already.</param>
    public QueryBillability(Guid eventId, long evaluationId, QuestLabResult questLabResult = null)
    {
        EventId = eventId;
        EvaluationId = evaluationId;
        QuestLabResult = questLabResult;
    }
}

[ExcludeFromCodeCoverage]
public class QueryBillabilityResult(bool isBillable, bool isLabResultsReceived)
{
    public bool IsBillable { get; } = isBillable;
    public bool  IsLabResultsReceived { get; } = isLabResultsReceived;
}

public class QueryBillabilityHandler(
    ILogger<QueryBillabilityHandler> logger,
    IMediator mediator)
    : IRequestHandler<QueryBillability, QueryBillabilityResult>
{
    private readonly ILogger _logger = logger;

    public async Task<QueryBillabilityResult> Handle(QueryBillability request, CancellationToken cancellationToken)
    {
        var results = request.QuestLabResult ?? await mediator.Send(new QueryQuestLabResultByEvaluationId(request.EvaluationId), cancellationToken);
        if (results != null) // Lab was performed and we have results
        {
            var isBillable = IsBillable(results, request);

            if (!isBillable)
                _logger.LogInformation("EventId={EventId} is not billable, for EvaluationId={EvaluationId}", request.EventId, request.EvaluationId);

            return new QueryBillabilityResult(isBillable, true);
        }

        var notPerformed = await mediator.Send(new QueryExamNotPerformed(request.EvaluationId), cancellationToken);
        if (notPerformed != null)
        {
            _logger.LogInformation("EventId={EventId} is not billable, exam was not performed, for EvaluationId={EvaluationId}", request.EventId, request.EvaluationId);
            return new QueryBillabilityResult(false, false);
        }

        // Should not normally get here, something must have gone wrong when inserting this exam to db
        _logger.LogWarning("Unable to determine if EvaluationId={EvaluationId} is billable because we do not have results nor details that it was not performed, for EventId={EventId}",
            request.EvaluationId, request.EventId);

        return new QueryBillabilityResult(false, false);
    }

    private bool IsBillable(QuestLabResult result, QueryBillability request)
    {
        if (result.NormalityCode is NormalityCodes.Normal or NormalityCodes.Abnormal)
            return true;

        if (result.NormalityCode != NormalityCodes.Undetermined)
            _logger.LogWarning("Invalid Normality={Normality}, setting billability to false, for EvaluationId={EvaluationId} with EventId={EventId}", result.Normality, request.EvaluationId, request.EventId);

        return false;
    }
}