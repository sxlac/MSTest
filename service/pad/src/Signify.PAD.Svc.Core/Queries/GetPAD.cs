using MediatR;
using Microsoft.EntityFrameworkCore;
using NewRelic.Api.Agent;
using Signify.PAD.Svc.Core.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.PAD.Svc.Core.Queries
{
    /// <summary>
    /// Get PAD details from database
    /// </summary>
    public class GetPAD : IRequest<Data.Entities.PAD>
    {
        public long EvaluationId { get; set; }
    }

    public class GetPADHandler : IRequestHandler<GetPAD, Data.Entities.PAD>
    {
        private readonly PADDataContext _dataContext;

        public GetPADHandler(PADDataContext dataContext)
        {
            _dataContext = dataContext;
        }

        [Trace]
        public Task<Data.Entities.PAD> Handle(GetPAD request, CancellationToken cancellationToken)
        {
            return _dataContext.PAD
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.EvaluationId == request.EvaluationId, cancellationToken);
        }
    }
}
