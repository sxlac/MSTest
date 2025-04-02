using MediatR;
using Microsoft.EntityFrameworkCore;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.Spirometry.Core.Queries;

/// <summary>
/// Query to search for <see cref="ProviderPay"/> by a given EvaluationId, if one exists
/// </summary>
public class QueryProviderPay : IRequest<ProviderPay>
{
    public long EvaluationId { get; }

    public QueryProviderPay(long evaluationId)
    {
        EvaluationId = evaluationId;
    }
}

public class QueryProviderPayHandler : IRequestHandler<QueryProviderPay, ProviderPay>
{
    private readonly SpirometryDataContext _dataContext;

    public QueryProviderPayHandler(SpirometryDataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public async Task<ProviderPay> Handle(QueryProviderPay request, CancellationToken cancellationToken)
    {
        var entity = await _dataContext.ProviderPays
            .AsNoTracking()
            .Include(t=>t.SpirometryExam)
            .FirstOrDefaultAsync(each => each.SpirometryExam.EvaluationId == request.EvaluationId, cancellationToken);

        return entity;
    }
}