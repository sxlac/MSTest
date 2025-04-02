using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Data;
using Signify.uACR.Core.Models;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Signify.uACR.Core.Queries;

[ExcludeFromCodeCoverage]
public class QueryPayableCdiStatus : IRequest<ExamStatus>
{
    public long EvaluationId { get; set; }
}

public class QueryPayableCdiStatusHandler(ILogger<QueryPayableCdiStatusHandler> logger, DataContext uAcrDataContext)
    : IRequestHandler<QueryPayableCdiStatus, ExamStatus>
{
    [Transaction]
    public async Task<ExamStatus> Handle(QueryPayableCdiStatus request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Looking for valid cdi_events for EvaluationId={EvaluationId}", request.EvaluationId);
        var latestCdiEvent = await uAcrDataContext.ExamStatuses.AsNoTracking()
            .Include(s => s.Exam)
            .Where(s => s.Exam.EvaluationId == request.EvaluationId)
            .FirstOrDefaultAsync(s =>
                    s.ExamStatusCodeId == (int) StatusCode.CdiPassedReceived ||
                    s.ExamStatusCodeId == (int) StatusCode.CdiFailedWithPayReceived,
                cancellationToken);
        return latestCdiEvent;
    }
}