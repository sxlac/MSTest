using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;

namespace Signify.DEE.Svc.Core.Messages.Queries;

public class GetLatestCdiEvent : IRequest<ExamStatus>
{
    public long EvaluationId { get; set; }
}

public class GetLatestCdiEventHandler(ILogger<GetLatestCdiEventHandler> logger, DataContext context)
    : IRequestHandler<GetLatestCdiEvent, ExamStatus>
{
    [Transaction]
    public async Task<ExamStatus> Handle(GetLatestCdiEvent request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Looking for valid cdi_events for EvaluationId={EvaluationId}", request.EvaluationId);
        var latestCdiEvent = await context.ExamStatuses.AsNoTracking().Include(s => s.Exam)
            .OrderByDescending(s => s.ExamStatusId)
            .Where(s => s.Exam.EvaluationId == request.EvaluationId)
            .FirstOrDefaultAsync(s =>
                    s.ExamStatusCodeId == ExamStatusCode.CdiPassedReceived.ExamStatusCodeId ||
                    s.ExamStatusCodeId == ExamStatusCode.CdiFailedWithPayReceived.ExamStatusCodeId ||
                    s.ExamStatusCodeId == ExamStatusCode.CdiFailedWithoutPayReceived.ExamStatusCodeId,
                cancellationToken);
        return latestCdiEvent;
    }
}