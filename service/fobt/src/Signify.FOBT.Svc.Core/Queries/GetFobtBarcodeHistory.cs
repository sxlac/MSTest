using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.FOBT.Svc.Core.Data;
using Signify.FOBT.Svc.Core.Data.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.FOBT.Svc.Core.Queries
{
    public class GetFobtBarcodeHistory : IRequest<FOBTBarcodeHistory>
    {
        public string Barcode { get; set; }

        public Guid OrderCorrelationId { get; set; }
    }

    public class GetFobtBarcodeHistoryHandler : IRequestHandler<GetFobtBarcodeHistory, FOBTBarcodeHistory>
    {
        private readonly ILogger<GetFobtBarcodeHistoryHandler> _logger;
        private readonly FOBTDataContext _dataContext;

        public GetFobtBarcodeHistoryHandler(ILogger<GetFobtBarcodeHistoryHandler> logger, FOBTDataContext dataContext)
        {
            _logger = logger;
            _dataContext = dataContext;
        }

        public async Task<FOBTBarcodeHistory> Handle(GetFobtBarcodeHistory request, CancellationToken cancellationToken)
        {
            try
            {
                return await _dataContext.FOBTBarcodeHistory.AsNoTracking().FirstOrDefaultAsync(x => x.Barcode == request.Barcode && x.OrderCorrelationId == request.OrderCorrelationId, cancellationToken: cancellationToken);
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Error looking up FOBT Barcode History Table with Barcode:{Barcode} and Order Correlation Id:{OrderCorrelationId}", request.Barcode, request.OrderCorrelationId);
            }

            return null;
        }
    }

}
