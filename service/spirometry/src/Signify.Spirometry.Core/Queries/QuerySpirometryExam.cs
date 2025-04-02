using MediatR;
using Microsoft.EntityFrameworkCore;
using NewRelic.Api.Agent;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.Spirometry.Core.Queries;

/// <summary>
/// Request to query the database (without tracking) for a <see cref="SpirometryExam"/> by the given <see cref="EvaluationId"/>
/// </summary>
public class QuerySpirometryExam : IRequest<SpirometryExam>
{
    public long EvaluationId { get; }

    /// <summary>
    /// Whether to include `SpirometryExamResult`
    /// </summary>
    public bool IncludeResults { get; set; }

    /// <summary>
    /// Whether to include `ClarificationFlag`
    /// </summary>
    public bool IncludeClarificationFlag { get; set; }

    public QuerySpirometryExam(long evaluationId)
    {
        EvaluationId = evaluationId;
    }
}

public class QuerySpirometryExamHandler : IRequestHandler<QuerySpirometryExam, SpirometryExam>
{
    private readonly SpirometryDataContext _spirometryDataContext;

    public QuerySpirometryExamHandler(SpirometryDataContext spirometryDataContext)
    {
        _spirometryDataContext = spirometryDataContext;
    }

    [Transaction]
    public async Task<SpirometryExam> Handle(QuerySpirometryExam request, CancellationToken cancellationToken)
    {
        var queryable = _spirometryDataContext.SpirometryExams
            .AsNoTracking();

        if (request.IncludeResults)
            queryable = queryable.Include(exam => exam.SpirometryExamResult);

        if (request.IncludeClarificationFlag)
            queryable = queryable.Include(exam => exam.ClarificationFlag);

        return await queryable
            .FirstOrDefaultAsync(each => each.EvaluationId == request.EvaluationId, cancellationToken);
    }
}