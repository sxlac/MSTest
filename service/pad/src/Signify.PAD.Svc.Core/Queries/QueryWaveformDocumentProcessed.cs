using MediatR;
using Microsoft.Extensions.Logging;
using Signify.PAD.Svc.Core.Data.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.PAD.Svc.Core.Queries;

/// <summary>
/// Query to determine whether the waveform document with the given filename has been processed
/// </summary>
public class QueryWaveformDocumentProcessed : IRequest<bool>
{
    public string Filename { get; }

    public QueryWaveformDocumentProcessed(string filename)
    {
        Filename = filename;
    }
}

public class QueryWaveformDocumentProcessedHandler : IRequestHandler<QueryWaveformDocumentProcessed, bool>
{
    private readonly ILogger _logger;
    private readonly IMediator _mediator;

    public QueryWaveformDocumentProcessedHandler(ILogger<QueryWaveformDocumentProcessedHandler> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    public async Task<bool> Handle(QueryWaveformDocumentProcessed request, CancellationToken cancellationToken)
    {
        try
        {
            var waveformDocument = await _mediator.Send(new GetWaveformDocumentByFilename {Filename = request.Filename}, cancellationToken);
            if (waveformDocument is null)
            {
                _logger.LogWarning("No WaveformDocument found by filename");
                return false;
            }

            var pad = await _mediator.Send(new GetPadByMemberPlanId(waveformDocument.MemberPlanId, waveformDocument.DateOfExam), cancellationToken);
            if (pad is null)
            {
                _logger.LogWarning("Failed to find PAD associated with file {WaveformDocumentId}", waveformDocument.WaveformDocumentId);
                return false;
            }

            return await _mediator.Send(new QueryPadStatusCode(pad.PADId, PADStatusCode.WaveformDocumentUploaded), cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to determine if the file has been processed - {Message}", e.Message);
        }
        return false;
    }
}