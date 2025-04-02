using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.HBA1CPOC.Svc.Core.Data;
using Signify.HBA1CPOC.Svc.Core.Data.Entities;

namespace Signify.HBA1CPOC.Svc.Core.Commands
{
    public class CreateOrUpdatePDFToClient : IRequest<PDFToClient>
    {
        public int PDFDeliverId { get; set; }
        public Guid EventId { get; set; }
        public long EvaluationId { get; set; }
        public DateTime DeliveryDateTime { get; set; }
        public DateTime DeliveryCreatedDateTime { get; set; }
        public long BatchId { get; set; }
        public string BatchName { get; set; }
        public int HBA1CPOCId { get; set; }
    }

    public class CreateOrUpdatePDFToClientHandler : IRequestHandler<CreateOrUpdatePDFToClient, PDFToClient>
    {
        private readonly Hba1CpocDataContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateOrUpdatePDFToClientHandler> _logger;

        public CreateOrUpdatePDFToClientHandler(Hba1CpocDataContext context, IMapper mapper, ILogger<CreateOrUpdatePDFToClientHandler> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<PDFToClient> Handle(CreateOrUpdatePDFToClient request, CancellationToken cancellationToken)
        {
            var a1cpocpdf = _mapper.Map<PDFToClient>(request);
            if (request.PDFDeliverId == 0)
            {
                // Create new entry in PDFToClient table
                var newA1cpocpdf = await _context.PDFToClient.AddAsync(a1cpocpdf, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                return newA1cpocpdf.Entity;
            }
            else
            {
                // Update existing PDFToClient entry
                _logger.LogDebug($"Updating PDFToClient PDFDeliverId:{a1cpocpdf.PDFDeliverId}");
                var updateA1cpoc = _context.PDFToClient.Update(a1cpocpdf);
                await _context.SaveChangesAsync(cancellationToken);
                return updateA1cpoc.Entity;
            }
        }
    }
}
