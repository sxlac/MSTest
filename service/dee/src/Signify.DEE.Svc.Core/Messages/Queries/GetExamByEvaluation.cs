using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.Messages.Queries;

public class GetExamByEvaluation : IRequest<Exam>
{
    public long EvaluationId { get; set; }

    /// <summary>
    /// Whether to include statuses with the entity
    /// </summary>
    public bool IncludeStatuses { get; init; }
}

public class GetExamByEvaluationHandler(ILogger<GetExamByEvaluationHandler> log, DataContext context)
    : IRequestHandler<GetExamByEvaluation, Exam>
{
    [Trace]
    public Task<Exam> Handle(GetExamByEvaluation request, CancellationToken cancellationToken)
    {
        log.LogDebug("{request} -- Exam lookup", request);
        var queryable = context.Exams.Include(e => e.EvaluationObjective).AsNoTracking();
        if (request.IncludeStatuses)
        {
            queryable = queryable.Include(e => e.ExamStatuses);
        }

        return queryable.FirstOrDefaultAsync(s => s.EvaluationId == request.EvaluationId, cancellationToken);
    }
}