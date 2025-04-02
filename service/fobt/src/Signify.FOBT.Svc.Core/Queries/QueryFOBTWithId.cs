using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NewRelic.Api.Agent;
using Signify.FOBT.Svc.Core.Data;

namespace Signify.FOBT.Svc.Core.Queries;

[ExcludeFromCodeCoverage]
public class QueryFOBTWithId : IRequest<QueryFOBTResponse>
{
    public int FOBTId { get; set; }
}

/// <summary>
/// Query FOBT handler
/// </summary>
public class QueryFOBTWithIdHandler : IRequestHandler<QueryFOBTWithId, QueryFOBTResponse>
{
    private readonly FOBTDataContext _dataContext;
    public QueryFOBTWithIdHandler(FOBTDataContext dataContext)
    {
        _dataContext = dataContext;
    }

    [Trace]
    public Task<QueryFOBTResponse> Handle(QueryFOBTWithId request, CancellationToken cancellationToken)
    {
        //Get by FOBTId
        var fobt = _dataContext.FOBT.FirstOrDefault(s => s.FOBTId == request.FOBTId);
        return fobt == null ?
            Task.FromResult(new QueryFOBTResponse { FOBT = null, Status = QueryFOBTStatus.NotFound }) :
            Task.FromResult(new QueryFOBTResponse { FOBT = fobt, Status = QueryFOBTStatus.BarcodeExists });
    }
}