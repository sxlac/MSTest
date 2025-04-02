using AutoMapper;
using MediatR;
using Signify.FOBT.Svc.Core.Data.Entities;
using Signify.FOBT.Svc.Core.Events;
using System.Threading;
using System.Threading.Tasks;
using Fobt = Signify.FOBT.Svc.Core.Data.Entities.FOBT;

namespace Signify.FOBT.Svc.Core.Commands;

public class InsertPdfToClient : IRequest
{
    public PdfDeliveredToClient PdfDeliveredToClient { get; set; }
    public Fobt Fobt { get; set; }
}

public class InsertPdfToClientHandler : IRequestHandler<InsertPdfToClient>
{
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;

    public InsertPdfToClientHandler(IMapper mapper, IMediator mediator)
    {
        _mapper = mapper;
        _mediator = mediator;
    }

    public async Task Handle(InsertPdfToClient request, CancellationToken cancellationToken)
    {
        var pdfDeliveryReceived = _mapper.Map<CreateOrUpdatePDFToClient>(request.PdfDeliveredToClient);
        pdfDeliveryReceived.FOBTId = request.Fobt.FOBTId;

        await _mediator.Send(pdfDeliveryReceived, cancellationToken);
        await _mediator.Send(new CreateFOBTStatus
        {
            FOBT = request.Fobt,
            StatusCode = FOBTStatusCode.ClientPDFDelivered
        }, cancellationToken);
    }
}