using MediatR;
using Microsoft.EntityFrameworkCore;
using Signify.HBA1CPOC.Svc.Core.Data;
using Signify.HBA1CPOC.Svc.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.HBA1CPOC.Svc.Core.Queries
{
    public class GetHba1CPocStatus : IRequest<HBA1CPOCStatus>
    {
        public HBA1CPOCStatusCode StatusCode { get; set; }
        public long EvaluationId { get; set; }
    }

    public class GetHba1CPocStatusHandler : IRequestHandler<GetHba1CPocStatus, HBA1CPOCStatus>
    {
        private readonly Hba1CpocDataContext _dataContext;

        public GetHba1CPocStatusHandler(Hba1CpocDataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public Task<HBA1CPOCStatus> Handle(GetHba1CPocStatus request, CancellationToken cancellationToken)
        {
            return _dataContext
                        .HBA1CPOCStatus
                        .Include(x => x.HBA1CPOC)
                        .Include(x => x.HBA1CPOCStatusCode)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.HBA1CPOCStatusCode == request.StatusCode &&
                                                x.HBA1CPOC.EvaluationId == request.EvaluationId,
                                                cancellationToken);
        }
    }
}
