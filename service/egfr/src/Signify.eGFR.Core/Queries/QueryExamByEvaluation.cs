using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.eGFR.Core.Data;
using Signify.eGFR.Core.Data.Entities;

namespace Signify.eGFR.Core.Queries;

[ExcludeFromCodeCoverage]
public class QueryExamByEvaluation : IRequest<Exam>
{
    public long EvaluationId { get; set; }

    /// <summary>
    /// Whether to include statuses with the entity
    /// </summary>
    public bool IncludeStatuses { get; init; }
}

public class QueryExamByEvaluationHandler(ILogger<QueryExamByEvaluationHandler> logger, DataContext dataContext)
    : IRequestHandler<QueryExamByEvaluation, Exam>
{
    private readonly ILogger _logger = logger;

    [Trace]
    public Task<Exam> Handle(QueryExamByEvaluation request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("QueryExamByEvaluation with IncludeStatuses {IncludeStatuses} for Evaluation: {EvaluationId}", request.IncludeStatuses, request.EvaluationId);
        var queryable = dataContext.Exams.AsNoTracking();
        if (request.IncludeStatuses)
        {
            queryable = queryable.Include(e => e.ExamStatuses);
        }

        return queryable.FirstOrDefaultAsync(s => s.EvaluationId == request.EvaluationId, cancellationToken);
    }
}