using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NewRelic.Api.Agent;
using Signify.A1C.Svc.Core.Data;

namespace Signify.A1C.Svc.Core.Queries
{
    public class QueryA1CWithId : IRequest<QueryA1CResponse>
    {
        public int HBA1CId { get; set; }
    }

    /// <summary>
    /// Query A1C handler
    /// </summary>
    public class QueryA1CWithIdHandler : IRequestHandler<QueryA1CWithId, QueryA1CResponse>
    {
        private readonly A1CDataContext _dataContext;
        public QueryA1CWithIdHandler(A1CDataContext dataContext)
        {
            _dataContext = dataContext;
        }

        [Trace]
        public Task<QueryA1CResponse> Handle(QueryA1CWithId request, CancellationToken cancellationToken)
        {
            //Get by A1CId
            var a1C = _dataContext.A1C.FirstOrDefault(s => s.A1CId == request.HBA1CId);
            return a1C == null ?
                 Task.FromResult(new QueryA1CResponse { A1C = null, Status = QueryA1CStatus.NotFound }) :
                 Task.FromResult(new QueryA1CResponse { A1C = a1C, Status = QueryA1CStatus.BarcodeExists });
        }
    }
}
