using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.A1C.Svc.Core.Data;
using Signify.A1C.Svc.Core.Data.Entities;

namespace Signify.A1C.Svc.Core.Commands
{
    public class UpdateBarcodeHistory : IRequest<bool>
    {
        public int A1CId { get; set; }
        public int EvaluationId { get; set; }
        public string Barcode { get; set; }
    }

    public class UpdateBarcodeHistoryHandler : IRequestHandler<UpdateBarcodeHistory, bool>
    {
        private readonly A1CDataContext _context;
        private readonly ILogger<UpdateBarcodeHistoryHandler> _logger;

        public UpdateBarcodeHistoryHandler(A1CDataContext context, ILogger<UpdateBarcodeHistoryHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        [Trace]
        public async Task<bool> Handle(UpdateBarcodeHistory request, CancellationToken cancellationToken)
        {
            if (request.A1CId == 0)
            {
                _logger.LogDebug($"Invalid A1CId, EvaluationId: {request.EvaluationId}, A1CId: {request.A1CId}, BarCode: {request.Barcode}");
                return false;
            }
            A1CBarcodeHistory barcodeHistory = new A1CBarcodeHistory
            {
                A1CId = request.A1CId,
                Barcode = request.Barcode,
                CreatedDateTime = DateTimeOffset.UtcNow
            };

            //Create A1C Barcode History row

            await _context.A1CBarcodeHistory.AddAsync(barcodeHistory, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}