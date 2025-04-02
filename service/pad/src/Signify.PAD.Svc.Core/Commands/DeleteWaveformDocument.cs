using MediatR;
using Microsoft.EntityFrameworkCore;
using Signify.PAD.Svc.Core.Data;
using Signify.PAD.Svc.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.PAD.Svc.Core.Commands;

public class DeleteWaveformDocument : IRequest
{
    public WaveformDocument Waveform { get; }

    public DeleteWaveformDocument(WaveformDocument waveform)
    {
        Waveform = waveform;
    }
}

public class DeleteWaveformDocumentHandler : IRequestHandler<DeleteWaveformDocument>
{
    private readonly PADDataContext _context;

    public DeleteWaveformDocumentHandler(PADDataContext context)
    {
        _context = context;
    }

    public Task Handle(DeleteWaveformDocument request, CancellationToken cancellationToken)
    {
        _context.WaveformDocument
            .Entry(request.Waveform)
            .State = EntityState.Deleted;

        return _context.SaveChangesAsync(cancellationToken);
    }
}
