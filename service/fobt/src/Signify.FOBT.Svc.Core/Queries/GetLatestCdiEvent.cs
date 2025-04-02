using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.FOBT.Svc.Core.Data;
using Signify.FOBT.Svc.Core.Data.Entities;

namespace Signify.FOBT.Svc.Core.Queries;

[ExcludeFromCodeCoverage]
public class GetLatestCdiEvent : IRequest<FOBTStatus>
{
    public int EvaluationId { get; set; }
}

public class GetLatestCdiEventHandler : IRequestHandler<GetLatestCdiEvent, FOBTStatus>
{
    private readonly ILogger _logger;
    private readonly FOBTDataContext _context;

    public GetLatestCdiEventHandler(ILogger<GetLatestCdiEventHandler> logger, FOBTDataContext context)
    {
        _logger = logger;
        _context = context;
    }

    [Transaction]
    public async Task<FOBTStatus> Handle(GetLatestCdiEvent request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Looking for valid cdi_events for EvaluationId={EvaluationId}", request.EvaluationId);
        var latestCdiEvent = await _context.FOBTStatus.AsNoTracking().Include(s => s.FOBT)
            .OrderByDescending(s => s.FOBTStatusId)
            .Where(s => s.FOBT.EvaluationId == request.EvaluationId)
            .FirstOrDefaultAsync(s =>
                    s.FOBTStatusCodeId == (int) FOBTStatusCode.StatusCodes.CdiPassedReceived ||
                    s.FOBTStatusCodeId == (int) FOBTStatusCode.StatusCodes.CdiFailedWithPayReceived ||
                    s.FOBTStatusCodeId == (int) FOBTStatusCode.StatusCodes.CdiFailedWithoutPayReceived,
                cancellationToken);
        return latestCdiEvent;
    }
}