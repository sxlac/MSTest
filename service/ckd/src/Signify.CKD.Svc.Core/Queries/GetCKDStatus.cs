using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using Signify.CKD.Svc.Core.Data;
using Signify.CKD.Svc.Core.Data.Entities;

namespace Signify.CKD.Svc.Core.Queries;

public class GetCKDStatus : IRequest<CKDStatus>
{
    public CKDStatusCode StatusCode { get; set; }
    public long EvaluationId { get; set; }
}

public class GetHba1CPocStatusHandler : IRequestHandler<GetCKDStatus, CKDStatus>
{
    private readonly CKDDataContext _dataContext;

    public GetHba1CPocStatusHandler(CKDDataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public Task<CKDStatus> Handle(GetCKDStatus request, CancellationToken cancellationToken)
    {
        return _dataContext
            .CKDStatus
            .Include(x => x.CKD)
            .Include(x => x.CKDStatusCode)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CKDStatusCode == request.StatusCode &&
                                      x.CKD.EvaluationId == request.EvaluationId,
                cancellationToken);
    }
}