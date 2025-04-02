using MediatR;
using Microsoft.EntityFrameworkCore;
using Signify.eGFR.Core.Data;
using Signify.eGFR.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.eGFR.Core.Queries;

/// <summary>
/// Request to query the database (without tracking) for a <see cref="Exam"/> by the given <see cref="EvaluationId"/>
/// </summary>
public class QueryExam(long evaluationId) : IRequest<Exam>
{
    public long EvaluationId { get; } = evaluationId;
}

public class QueryExamHandler(DataContext dataContext) : IRequestHandler<QueryExam, Exam>
{
    public async Task<Exam> Handle(QueryExam request, CancellationToken cancellationToken)
    {
        return await dataContext.Exams
            .AsNoTracking()
            .FirstOrDefaultAsync(each => each.EvaluationId == request.EvaluationId, cancellationToken)
            .ConfigureAwait(false);
    }
}