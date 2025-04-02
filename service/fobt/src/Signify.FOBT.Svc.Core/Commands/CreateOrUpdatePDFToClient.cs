using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.FOBT.Svc.Core.Data;
using Signify.FOBT.Svc.Core.Data.Entities;

namespace Signify.FOBT.Svc.Core.Commands;

public class CreateOrUpdatePDFToClient : IRequest<PDFToClient>
{
    public int PDFDeliverId { get; set; }
    public Guid EventId { get; set; }
    public long EvaluationId { get; set; }
    public DateTime DeliveryDateTime { get; set; }
    public DateTime DeliveryCreatedDateTime { get; set; }
    public long BatchId { get; set; }
    public string BatchName { get; set; }
    public int FOBTId { get; set; }
}

public class CreateOrUpdatePDFToClientHandler : IRequestHandler<CreateOrUpdatePDFToClient, PDFToClient>
{
    private readonly FOBTDataContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateOrUpdatePDFToClientHandler> _logger;

    public CreateOrUpdatePDFToClientHandler(FOBTDataContext context, IMapper mapper, ILogger<CreateOrUpdatePDFToClientHandler> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PDFToClient> Handle(CreateOrUpdatePDFToClient request, CancellationToken cancellationToken)
    {
        var fobtPdf = _mapper.Map<PDFToClient>(request);
        if (request.PDFDeliverId == 0)
        {
            //Create new 
            fobtPdf.CreatedDateTime = DateTime.UtcNow;
            var newFobtPdf = await _context.PDFToClient.AddAsync(fobtPdf, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return newFobtPdf.Entity;
        }
        //update

        _logger.LogDebug("Updating PDFToClient PDFDeliverId:{PdfDeliverId}", fobtPdf.PDFDeliverId);
        var updateFobt = _context.PDFToClient.Update(fobtPdf);
        //update FOBT status
        await _context.SaveChangesAsync(cancellationToken);
        return updateFobt.Entity;
    }
}