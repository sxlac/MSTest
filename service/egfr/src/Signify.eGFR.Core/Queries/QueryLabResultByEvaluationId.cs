using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Signify.eGFR.Core.Data;
using Signify.eGFR.Core.Data.Entities;

namespace Signify.eGFR.Core.Queries;

[ExcludeFromCodeCoverage]
public class QueryLabResultByEvaluationId(long evaluationId) : IRequest<LabResult>
{
    public long EvaluationId { get; } = evaluationId;
}

public class QueryLabResultByEvaluationIdHandler(DataContext dataContext)
    : IRequestHandler<QueryLabResultByEvaluationId, LabResult>
{
    public async Task<LabResult> Handle(QueryLabResultByEvaluationId request, CancellationToken cancellationToken)
    {
        var results = (from lr in dataContext.LabResults
            join e in dataContext.Exams on new { cId = lr.ExamId,  } equals new { cId = e.ExamId }
            where e.EvaluationId == request.EvaluationId
            orderby lr.LabResultId descending 
            select lr).FirstOrDefault();

        return await Task.FromResult(results);
    }
}