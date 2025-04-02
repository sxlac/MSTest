using MediatR;
using Microsoft.EntityFrameworkCore;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Data;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Threading;

namespace Signify.uACR.Core.Queries;

/// <summary>
/// Query to search for <see cref="ExamNotPerformed"/> by a given EvaluationId, if one exists
/// </summary>
[ExcludeFromCodeCoverage]
public class QueryExamNotPerformed(long evaluationId) : IRequest<ExamNotPerformed>
{
    public long EvaluationId { get; } = evaluationId;
}

public class QueryExamNotPerformedHandler(DataContext dataContext)
    : IRequestHandler<QueryExamNotPerformed, ExamNotPerformed>
{
    public async Task<ExamNotPerformed> Handle(QueryExamNotPerformed request, CancellationToken cancellationToken)
        => await dataContext.ExamNotPerformeds
            .AsNoTracking()
            .Include(x => x.Exam)
            .Include(x => x.NotPerformedReason)
            .FirstOrDefaultAsync(each => each.Exam.EvaluationId == request.EvaluationId,
                cancellationToken);
}