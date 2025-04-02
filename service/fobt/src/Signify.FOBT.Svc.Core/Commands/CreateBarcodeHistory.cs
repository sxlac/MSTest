using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NewRelic.Api.Agent;
using Signify.FOBT.Svc.Core.Data;
using Signify.FOBT.Svc.Core.Data.Entities;

namespace Signify.FOBT.Svc.Core.Commands;

[ExcludeFromCodeCoverage]
public class CreateBarcodeHistory : IRequest<Unit>
{
    public int FOBTId { get; set; }
    public Guid? OrderCorrelationId { get; set; }
    public string Barcode { get; set; }
}

public class CreateBarcodeHistoryHandler : IRequestHandler<CreateBarcodeHistory, Unit>
{
    private readonly FOBTDataContext _context;

    public CreateBarcodeHistoryHandler(FOBTDataContext context)
    {
        _context = context;
    }

    [Trace]
    public async Task<Unit> Handle(CreateBarcodeHistory request, CancellationToken cancellationToken)
    {
        var history = new FOBTBarcodeHistory
        {
            FOBTId = request.FOBTId,
            Barcode = request.Barcode,
            CreatedDateTime = DateTimeOffset.UtcNow,
            OrderCorrelationId = request.OrderCorrelationId
        };

        await _context.FOBTBarcodeHistory.AddAsync(history, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}