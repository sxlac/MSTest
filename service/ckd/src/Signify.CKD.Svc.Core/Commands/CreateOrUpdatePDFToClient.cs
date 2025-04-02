using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Signify.CKD.Svc.Core.Data;
using Signify.CKD.Svc.Core.Data.Entities;

namespace Signify.CKD.Svc.Core.Commands
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
        public int CKDId { get; set; }
    }

    public class CreateOrUpdatePDFToClientHandler : IRequestHandler<CreateOrUpdatePDFToClient, PDFToClient>
    {
        private readonly CKDDataContext _context;
        private readonly IMapper _mapper;

        public CreateOrUpdatePDFToClientHandler(CKDDataContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PDFToClient> Handle(CreateOrUpdatePDFToClient request, CancellationToken cancellationToken)
        {
            var pdf = _mapper.Map<PDFToClient>(request);

            // Add or update the record
            var entry = request.PDFDeliverId < 1
                ? await _context.PDFToClient.AddAsync(pdf, cancellationToken)
                : _context.PDFToClient.Update(pdf);

            await _context.SaveChangesAsync(cancellationToken);
            return entry.Entity;
        }
    }
}
