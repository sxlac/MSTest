using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.PAD.Svc.Core.Data;
using Signify.PAD.Svc.Core.Data.Entities;

namespace Signify.PAD.Svc.Core.Commands;

[ExcludeFromCodeCoverage]
public class CreateOrUpdatePDFToClient : IRequest<PDFToClient>
{
    public int PDFDeliverId { get; set; }
    public Guid EventId { get; set; }
    public long EvaluationId { get; set; }
    public DateTime DeliveryDateTime { get; set; }
    public DateTime DeliveryCreatedDateTime { get; set; }
    public long BatchId { get; set; }
    public string BatchName { get; set; }
    public int PADId { get; set; }
}

public class CreateOrUpdatePDFToClientHandler : IRequestHandler<CreateOrUpdatePDFToClient, PDFToClient>
{
    private readonly PADDataContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateOrUpdatePDFToClientHandler> _logger;

    public CreateOrUpdatePDFToClientHandler(PADDataContext context, IMapper mapper, ILogger<CreateOrUpdatePDFToClientHandler> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PDFToClient> Handle(CreateOrUpdatePDFToClient request, CancellationToken cancellationToken)
    {
        var padpdf = _mapper.Map<PDFToClient>(request);
        if (request.PDFDeliverId == 0)
        {
            //Create new 
            var newPadpdf = await _context.PDFToClient.AddAsync(padpdf, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return newPadpdf.Entity;
        }
        //update
        else
        {
            _logger.LogDebug($"Updating PDFToClient PDFDeliverId:{padpdf.PDFDeliverId}");
            var updatePAD = _context.PDFToClient.Update(padpdf);
            //update PAD status
            await _context.SaveChangesAsync(cancellationToken);
            return updatePAD.Entity;
        }
    }
}