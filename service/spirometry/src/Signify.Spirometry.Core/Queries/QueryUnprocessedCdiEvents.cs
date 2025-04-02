using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewRelic.Api.Agent;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Data.Entities;
using StatusCode = Signify.Spirometry.Core.Models.StatusCode;

namespace Signify.Spirometry.Core.Queries;

public class QueryUnprocessedCdiEventForPayments : IRequest<IList<CdiEventForPayment>>
{
    public QueryUnprocessedCdiEventForPayments(long evaluationId)
    {
        EvaluationId = evaluationId;
    }

    public long EvaluationId { get; set; }
}

public class QueryUnprocessedCdiEventForPaymentsHandler : IRequestHandler<QueryUnprocessedCdiEventForPayments, IList<CdiEventForPayment>>
{
    private readonly SpirometryDataContext _dataContext;

    public QueryUnprocessedCdiEventForPaymentsHandler(SpirometryDataContext dataContext)
    {
        _dataContext = dataContext;
    }

    [Transaction]
    public async Task<IList<CdiEventForPayment>> Handle(QueryUnprocessedCdiEventForPayments request, CancellationToken cancellationToken)
    {
        var cdiEvents = await _dataContext.CdiEventForPayments
            .AsNoTracking()
            .Where(each => each.EvaluationId == request.EvaluationId)
            .ToListAsync(cancellationToken);

        var cdiEventsToProcess = await GetEventsToProcess(request, cdiEvents, cancellationToken);

        return cdiEventsToProcess;
    }

    /// <summary>
    /// Compares the list of CdiEvent received to the list of cdi related status saved in ExamStatus table
    /// and return the list of CdiEvent that have not been processed yet
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cdiEvents"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [Trace]
    private async Task<IList<CdiEventForPayment>> GetEventsToProcess(QueryUnprocessedCdiEventForPayments request, IEnumerable<CdiEventForPayment> cdiEvents, CancellationToken cancellationToken)
    {
        var cdiStatus = await _dataContext.ExamStatuses
            .AsNoTracking()
            .Include(each => each.SpirometryExam)
            .Where(each => each.SpirometryExam.EvaluationId == request.EvaluationId && (each.StatusCodeId == (int)StatusCode.CdiPassedReceived ||
                                                                                        each.StatusCodeId == (int)StatusCode.CdiFailedWithPayReceived ||
                                                                                        each.StatusCodeId == (int)StatusCode.CdiFailedWithoutPayReceived))
            .Select(each => each.StatusDateTime)
            .ToListAsync(cancellationToken);

        var unProcessed = cdiEvents.Where(each => !cdiStatus.Contains(each.DateTime.UtcDateTime)).ToList();

        return unProcessed;
    }
}