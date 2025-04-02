using MediatR;
using Microsoft.EntityFrameworkCore;
using Signify.PAD.Svc.Core.Data;
using Signify.PAD.Svc.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.PAD.Svc.Core.Queries;

public class QueryPadPerformedStatus : IRequest<QueryPadPerformedStatusResult>
{
    public int PadId { get; }

    public QueryPadPerformedStatus(int padId)
    {
        PadId = padId;
    }
}

public class QueryPadPerformedStatusResult
{
    /// <summary>
    /// Whether or not PAD was performed for the given evaluation
    /// </summary>
    /// <remarks>
    /// Null if neither Performed nor NotPerformed status is found for the given evaluation
    /// </remarks>
    public bool? IsPerformed { get; }

    public QueryPadPerformedStatusResult(bool? isPerformed)
    {
        IsPerformed = isPerformed;
    }
}

public class QueryPadPerformedStatusHandler : IRequestHandler<QueryPadPerformedStatus, QueryPadPerformedStatusResult>
{
    private readonly PADDataContext _dataContext;

    private static readonly int PerformedId = PADStatusCode.PadPerformed.PADStatusCodeId;
    private static readonly int NotPerformedId = PADStatusCode.PadNotPerformed.PADStatusCodeId;

    public QueryPadPerformedStatusHandler(PADDataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public async Task<QueryPadPerformedStatusResult> Handle(QueryPadPerformedStatus request, CancellationToken cancellationToken)
    {
        var result = await _dataContext.PADStatus
            .AsNoTracking()
            .FirstOrDefaultAsync(each =>
                    each.PADId == request.PadId &&
                    (each.PADStatusCodeId == PerformedId || each.PADStatusCodeId == NotPerformedId),
                cancellationToken);

        if (result == null)
            return new QueryPadPerformedStatusResult(null);

        return new QueryPadPerformedStatusResult(result.PADStatusCodeId == PerformedId);
    }
}