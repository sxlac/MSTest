using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Signify.uACR.Core.Data;
using Signify.uACR.Core.Data.Entities;

namespace Signify.uACR.Core.Queries;

public class QueryLabResultByEvaluationId(long evaluationId) : IRequest<LabResult>
{
    public long EvaluationId { get; } = evaluationId;
}

public class QueryLabResultByEvaluationIdHandler(DataContext dataContext)
    : IRequestHandler<QueryLabResultByEvaluationId, LabResult>
{
    public async Task<LabResult> Handle(QueryLabResultByEvaluationId request, CancellationToken cancellationToken)
        =>  await dataContext.LabResults
            .AsNoTracking()
            .FirstOrDefaultAsync(each => each.EvaluationId == request.EvaluationId, cancellationToken);
}
