using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.Messages.Queries;

public class GetExamResultByExamId : IRequest<ExamResult>
{
    public long ExamId { get; set; }

    /// <summary>
    /// Whether to include statuses with the entity
    /// </summary>
    public bool IncludeStatuses { get; init; }
}

public class GetExamResultByExamIdHandler(ILogger<GetExamResultByExamIdHandler> log, DataContext context)
    : IRequestHandler<GetExamResultByExamId, ExamResult>
{
    [Trace]
    public Task<ExamResult> Handle(GetExamResultByExamId request, CancellationToken cancellationToken)
    {
        log.LogDebug("{request} -- ExamResults lookup", request);
        var queryable = context.ExamResults.AsNoTracking();
        return queryable.FirstOrDefaultAsync(s => s.ExamId == request.ExamId, cancellationToken);
    }
}