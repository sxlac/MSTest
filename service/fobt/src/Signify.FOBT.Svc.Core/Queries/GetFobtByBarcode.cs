using MediatR;
using Microsoft.EntityFrameworkCore;
using Signify.FOBT.Svc.Core.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.FOBT.Svc.Core.Queries
{
    public class GetFobtByBarcode : IRequest<List<Data.Entities.FOBT>>
    {
        public string Barcode { get; set; }
    }

    public class GetFobtByBarcodeHandler : IRequestHandler<GetFobtByBarcode, List<Data.Entities.FOBT>>
    {
        private readonly FOBTDataContext _dataContext;
        
        public GetFobtByBarcodeHandler(FOBTDataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public Task<List<Data.Entities.FOBT>> Handle(GetFobtByBarcode request, CancellationToken cancellationToken)
        => _dataContext.FOBT
            .AsNoTracking()
            .Where(each => each.Barcode == request.Barcode)
            .ToListAsync(cancellationToken);
    }
}
