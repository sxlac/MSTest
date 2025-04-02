using MediatR;
using Microsoft.EntityFrameworkCore;
using Signify.FOBT.Svc.Core.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.FOBT.Svc.Core.Queries
{
    public class GetBarcodeHistory : IRequest<List<Data.Entities.FOBTBarcodeHistory>>
    {
        public string Barcode { get; set; }
    }

    public class GetBarcodeHistoryHandler(FOBTDataContext dataContext) : IRequestHandler<GetBarcodeHistory, List<Data.Entities.FOBTBarcodeHistory>>
    {
        private readonly FOBTDataContext _dataContext = dataContext;
      
        public Task<List<Data.Entities.FOBTBarcodeHistory>> Handle(GetBarcodeHistory request, CancellationToken cancellationToken)
        => _dataContext.FOBTBarcodeHistory
            .AsNoTracking()
            .Where(each => each.Barcode == request.Barcode)
            .ToListAsync(cancellationToken);
    }
}
