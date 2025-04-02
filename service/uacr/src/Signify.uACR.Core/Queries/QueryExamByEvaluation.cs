using MediatR;
using Microsoft.EntityFrameworkCore;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Data;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Threading;

namespace Signify.uACR.Core.Queries;

/// <summary>
/// Request to query the database (without tracking) for a <see cref="Exam"/> by the given <see cref="EvaluationId"/>
/// </summary>
[ExcludeFromCodeCoverage]
public class QueryExamByEvaluation : IRequest<Exam>
{
    public long EvaluationId { get; set; }
    
    /// <summary>
    /// Whether to include statuses with the entity
    /// </summary>
    public bool IncludeStatuses { get; init; }
}

public class GetExamByEvaluationHandler(DataContext dataContext) : IRequestHandler<QueryExamByEvaluation, Exam>
{
    public async Task<Exam> Handle(QueryExamByEvaluation request, CancellationToken cancellationToken)
    {
        var queryable = dataContext.Exams.AsNoTracking();
        if (request.IncludeStatuses)
        {
            queryable = queryable.Include(e => e.ExamStatuses);
        }

        return await queryable.FirstOrDefaultAsync(s => s.EvaluationId == request.EvaluationId, cancellationToken);
    }
}