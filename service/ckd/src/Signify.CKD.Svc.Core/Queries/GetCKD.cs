using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewRelic.Api.Agent;
using Signify.CKD.Svc.Core.Data;

namespace Signify.CKD.Svc.Core.Queries;

public class GetCKD : IRequest<Data.Entities.CKD>
{
    public long EvaluationId { get; set; }
}

public class GetCKDHandler : IRequestHandler<GetCKD, Data.Entities.CKD>
{
    private readonly CKDDataContext _dataContext;

    public GetCKDHandler(CKDDataContext dataContext)
    {
        _dataContext = dataContext;
    }

    [Trace]
    public Task<Data.Entities.CKD> Handle(GetCKD request, CancellationToken cancellationToken)
    {
        return _dataContext.CKD
            .AsNoTracking()
            .FirstOrDefaultAsync(ckd => ckd.EvaluationId == request.EvaluationId, cancellationToken);
    }
}