using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NewRelic.Api.Agent;
using Signify.A1C.Svc.Core.Data;

namespace Signify.A1C.Svc.Core.Queries
{

    public class QueryA1C : IRequest<QueryA1CResponse>
    {
        public int? AppointmentId { get; set; }
        public string Barcode { get; set; }
    }

    public class QueryA1CResponse
    {
        public Data.Entities.A1C A1C { get; set; }
        public QueryA1CStatus Status { get; set; }

    }

    public enum QueryA1CStatus
    {
        BarcodeExists,     // 0 Barcode  Exists
        NotFound,          // 1 Barcode and Appointment not found
        AppointmentFoundNoBarcode //2 Appointment Found and no barcode
    }

    /// <summary>
    /// Query A1C handler
    /// </summary>
    public class QueryA1CHandler : IRequestHandler<QueryA1C, QueryA1CResponse>
    {
        private readonly A1CDataContext _dataContext;
        public QueryA1CHandler(A1CDataContext dataContext)
        {
            _dataContext = dataContext;
        }

        [Trace]
        public Task<QueryA1CResponse> Handle(QueryA1C request, CancellationToken cancellationToken)
        {

            //Get by barcode
            var a1C = _dataContext.A1C.FirstOrDefault(s => s.Barcode == request.Barcode);
            if (a1C != null)
            {
                return Task.FromResult(new QueryA1CResponse { A1C = a1C, Status = QueryA1CStatus.BarcodeExists });
            }

            // Get by Appointment Id
            if (request?.AppointmentId > 0)
            {
                a1C = _dataContext.A1C.FirstOrDefault(s => s.AppointmentId == request.AppointmentId);
            }

            return a1C == null
                ? Task.FromResult(new QueryA1CResponse { A1C = null, Status = QueryA1CStatus.NotFound })
                : Task.FromResult(new QueryA1CResponse { A1C = a1C, Status = QueryA1CStatus.AppointmentFoundNoBarcode });
        }
    }

}
