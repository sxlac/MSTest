using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Signify.PAD.Svc.Core.Data;
using Signify.PAD.Svc.Core.Data.Entities;

namespace Signify.PAD.Svc.Core.Commands
{
    public class CreateWaveformDocument : IRequest<WaveformDocument>
    {
        public WaveformDocument Document { get; }

        public CreateWaveformDocument(WaveformDocument document)
        {
            Document = document;
        }
    }

    public class CreateWaveformDocumentHandler : IRequestHandler<CreateWaveformDocument, WaveformDocument>
    {
        private readonly PADDataContext _context;

        public CreateWaveformDocumentHandler(PADDataContext context)
        {
            _context = context;
        }

        public Task<WaveformDocument> Handle(CreateWaveformDocument request, CancellationToken cancellationToken)
            => CreateWaveformDocument(request, cancellationToken);

        private async Task<WaveformDocument> CreateWaveformDocument(CreateWaveformDocument request, CancellationToken cancellationToken)
        {
            var document = await _context.WaveformDocument.AddAsync(request.Document, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken); 
            return document.Entity;
        }
    }
}
