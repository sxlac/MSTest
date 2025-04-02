using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.FOBT.Svc.Core.Queries
{
    public class GetFobtByHistory : IRequest<Data.Entities.FOBT>
    {
        public string Barcode { get; set; }

        public Guid OrderCorrelationId { get; set; }
    }

    public class GetFobtByHistoryHandler : IRequestHandler<GetFobtByHistory, Data.Entities.FOBT>
    {
        private readonly ILogger<GetFobtByHistoryHandler> _logger;
        private readonly IMediator _mediator;

        public GetFobtByHistoryHandler(IMediator mediator, ILogger<GetFobtByHistoryHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<Data.Entities.FOBT> Handle(GetFobtByHistory request, CancellationToken cancellationToken)
        {
            try
            {
                var fobtBarcodeHistoryTable = await _mediator.Send(new GetFobtBarcodeHistory { Barcode = request.Barcode, OrderCorrelationId = request.OrderCorrelationId }, cancellationToken: cancellationToken);

                if (fobtBarcodeHistoryTable != null && fobtBarcodeHistoryTable.FOBTId > 0)
                {
                    return await _mediator.Send(new GetFobtByFobtId { FobtId = fobtBarcodeHistoryTable.FOBTId }, cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error looking up FOBT Barcode History Table with Barcode:{Barcode} and Order Correlation Id:{OrderCorrelationId}", request.Barcode, request.OrderCorrelationId);
                throw;
            }

            return null;
        }
    }
}
