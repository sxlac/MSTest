using MediatR;
using Microsoft.EntityFrameworkCore;
using Signify.eGFR.Core.Data;
using Signify.eGFR.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.eGFR.Core.Queries;

/// <summary>
/// Query to search for <see cref="ExamNotPerformed"/> by a given EvaluationId, if one exists
/// </summary>
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