using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.eGFR.Core.Data;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Models;

namespace Signify.eGFR.Core.Queries;

public class QueryPayableCdiStatus(long evaluationId) : IRequest<ExamStatus>
{
    public long EvaluationId { get; } = evaluationId;
}


public class QueryPayableCdiStatusHandler(ILogger<QueryPayableCdiStatusHandler> logger, DataContext eGfrDataContext)
    : IRequestHandler<QueryPayableCdiStatus, ExamStatus>
{
    [Transaction]
    public async Task<ExamStatus> Handle(QueryPayableCdiStatus request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Looking for valid cdi_events for EvaluationId={EvaluationId}", request.EvaluationId);
        var latestCdiEvent = await eGfrDataContext.ExamStatuses.AsNoTracking().Include(s => s.Exam)
            .Where(s => s.Exam.EvaluationId == request.EvaluationId)
            .FirstOrDefaultAsync(s =>
                    s.ExamStatusCodeId == (int)StatusCode.CdiPassedReceived ||
                    s.ExamStatusCodeId == (int)StatusCode.CdiFailedWithPayReceived,
                cancellationToken);
        return latestCdiEvent;
    }
}