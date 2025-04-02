using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using Signify.HBA1CPOC.Messages.Events;
using Signify.HBA1CPOC.Svc.Core.Data.Entities;
using HbA1cPoc = Signify.HBA1CPOC.Svc.Core.Data.Entities.HBA1CPOC;

namespace Signify.HBA1CPOC.Svc.Core.Commands;

public class InsertPdfToClientTransaction : IRequest<Unit>
{
    public PdfDeliveredToClient PdfDeliveredToClient { get; set; }

    public HbA1cPoc HbA1cPoc { get; set; }
}

public class InsertPdfToClientTransactionHandler : IRequestHandler<InsertPdfToClientTransaction, Unit>
{
    private readonly ILogger<InsertPdfToClientTransactionHandler> _logger;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;

    public InsertPdfToClientTransactionHandler(ILogger<InsertPdfToClientTransactionHandler> logger, IMapper mapper,
        IMediator mediator)
    {
        _logger = logger;
        _mapper = mapper;
        _mediator = mediator;
    }

    public async Task<Unit> Handle(InsertPdfToClientTransaction request, CancellationToken cancellationToken)
    {
        var pdfDeliveryReceived = _mapper.Map<CreateOrUpdatePDFToClient>(request.PdfDeliveredToClient);
        pdfDeliveryReceived.HBA1CPOCId = request.HbA1cPoc.HBA1CPOCId;

        await _mediator.Send(pdfDeliveryReceived, cancellationToken);

        _logger.LogDebug("End of {Handle}. {Table} table updated for EvaluationID:{EvaluationId}, EventId:{EventId}",
            nameof(InsertPdfToClientTransactionHandler), nameof(PDFToClient), request.PdfDeliveredToClient.EvaluationId, request.PdfDeliveredToClient.EventId);

        return Unit.Value;
    }
}