using MediatR;
using Microsoft.EntityFrameworkCore;
using Signify.FOBT.Svc.Core.Data;
using Signify.FOBT.Svc.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.FOBT.Svc.Core.Queries
{
    public class GetFobtStatusByStatusCodeAndEvaluationId : IRequest<FOBTStatus>
    {
        public FOBTStatusCode FobtStatusCode { get; set; }
        public int EvaluationId { get; set; }
    }

    public class GetFobtStatusByStatusCodeAndEvaluationIdHandler : IRequestHandler<GetFobtStatusByStatusCodeAndEvaluationId, FOBTStatus>
    { 
        private readonly FOBTDataContext _dataContext;

        public GetFobtStatusByStatusCodeAndEvaluationIdHandler(FOBTDataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public Task<FOBTStatus> Handle(GetFobtStatusByStatusCodeAndEvaluationId request, CancellationToken cancellationToken)
        {
            return _dataContext
                .FOBTStatus
                .Include(status => status.FOBT)
                .Include(status => status.FOBTStatusCode)
                .AsNoTracking()
                .FirstOrDefaultAsync(status =>
                        status.FOBTStatusCode == request.FobtStatusCode &&
                        status.FOBT.EvaluationId == request.EvaluationId,
                    cancellationToken);
        }
    }
}
