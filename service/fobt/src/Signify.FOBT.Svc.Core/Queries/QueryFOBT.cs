using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NewRelic.Api.Agent;
using Signify.FOBT.Svc.Core.Data;

namespace Signify.FOBT.Svc.Core.Queries;

public class QueryFOBT : IRequest<QueryFOBTResponse>
{
    public int? AppointmentId { get; set; }
    public string Barcode { get; set; }
}

public class QueryFOBTResponse
{
    public Data.Entities.FOBT FOBT { get; set; }
    public QueryFOBTStatus Status { get; set; }

}

public enum QueryFOBTStatus
{
    BarcodeExists,     // 0 Barcode  Exists
    NotFound,          // 1 Barcode and Appointment not found
    AppointmentFoundNoBarcode //2 Appointment Found and no barcode
}

/// <summary>
/// Query FOBT handler
/// </summary>
public class QueryFOBTHandler : IRequestHandler<QueryFOBT, QueryFOBTResponse>
{
    private readonly FOBTDataContext _dataContext;
    public QueryFOBTHandler(FOBTDataContext dataContext)
    {
        _dataContext = dataContext;
    }

    [Trace]
    public Task<QueryFOBTResponse> Handle(QueryFOBT request, CancellationToken cancellationToken)
    {
        //Get by barcode
        var fobt = _dataContext.FOBT.FirstOrDefault(s => s.Barcode == request.Barcode);
        if (fobt != null)
        {
            return Task.FromResult(new QueryFOBTResponse { FOBT = fobt, Status = QueryFOBTStatus.BarcodeExists });
        }

        // Get by Appointment Id
        if (request?.AppointmentId > 0)
        {
            fobt = _dataContext.FOBT.FirstOrDefault(s => s.AppointmentId == request.AppointmentId);
        }

        return fobt == null
            ? Task.FromResult(new QueryFOBTResponse { FOBT = null, Status = QueryFOBTStatus.NotFound })
            : Task.FromResult(new QueryFOBTResponse { FOBT = fobt, Status = QueryFOBTStatus.AppointmentFoundNoBarcode });
    }
}