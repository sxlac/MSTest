using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Signify.eGFR.Core.Data;
using Signify.eGFR.Core.Data.Entities;

namespace Signify.eGFR.Core.Queries;

public class QueryQuestLabResultByEvaluationId(long evaluationId) : IRequest<QuestLabResult>
{
    public long EvaluationId { get; } = evaluationId;
}

public class QueryQuestLabResultByEvaluationIdHandler(DataContext dataContext)
    : IRequestHandler<QueryQuestLabResultByEvaluationId, QuestLabResult>
{
    public async Task<QuestLabResult> Handle(QueryQuestLabResultByEvaluationId request,
        CancellationToken cancellationToken)
    {
        var results = (from lr in dataContext.QuestLabResults
            join e in dataContext.Exams on new {cId = lr.CenseoId, d = lr.CollectionDate.Value} equals new
                {cId = e.CenseoId, d = e.DateOfService.Value}
            where e.EvaluationId == request.EvaluationId
            orderby lr.LabResultId descending
            select lr).FirstOrDefault();

        return await Task.FromResult(results);
    }
}