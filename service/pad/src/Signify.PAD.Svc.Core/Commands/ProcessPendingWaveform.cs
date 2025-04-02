using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.Infrastructure;
using Signify.PAD.Svc.Core.Queries;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.PAD.Svc.Core.Commands;

public class ProcessPendingWaveform : IRequest<ProcessPendingWaveformResult>
{
    public WaveformDocumentVendor Vendor { get; init; }

    public string Filename { get; init; }

    public string FilePath { get; init; }
}

[ExcludeFromCodeCoverage]
public class ProcessPendingWaveformResult
{
    public bool IsSuccessful { get; private init; }

    public bool FileAlreadyUploaded { get; private init; }

    public bool IgnoreFile { get; private init; }

    public int? ClientId { get; private init; }

    public static ProcessPendingWaveformResult Success(int? clientId) => new() { IsSuccessful = true, ClientId = clientId };

    public static ProcessPendingWaveformResult Fail() => new() { IsSuccessful = false };

    public static ProcessPendingWaveformResult FailFileUploaded() => new() { FileAlreadyUploaded = true };

    public static ProcessPendingWaveformResult Ignore() => new() { IsSuccessful = false, IgnoreFile = true };
}

public class ProcessPendingWaveformHandler : IRequestHandler<ProcessPendingWaveform, ProcessPendingWaveformResult>
{
    private readonly ILogger _logger;
    private readonly IApplicationTime _applicationTime;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;

    public ProcessPendingWaveformHandler(ILogger<ProcessPendingWaveformHandler> logger,
        IApplicationTime applicationTime,
        IMapper mapper,
        IMediator mediator)
    {
        _logger = logger;
        _applicationTime = applicationTime;
        _mapper = mapper;
        _mediator = mediator;
    }

    [Transaction]
    public async Task<ProcessPendingWaveformResult> Handle(ProcessPendingWaveform request, CancellationToken cancellationToken)
    {
        try
        {
            // Verify we haven't already processed this file
            var result = await GetResultIfProcessed(request, cancellationToken);
            if (result != null)
                return result;

            return await ProcessFile(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to process file");
            return ProcessPendingWaveformResult.Fail();
        }
    }

    private async Task<ProcessPendingWaveformResult> GetResultIfProcessed(ProcessPendingWaveform request, CancellationToken cancellationToken)
    {
        var waveformDocument = await _mediator.Send(new GetWaveformDocumentByFilename
        {
            Filename = request.Filename
        }, cancellationToken);

        if (waveformDocument == null)
            return null; // File hasn't been processed yet

        // There must have been an issue moving the file from Pending to Processed; do not re-process
        _logger.LogInformation("WaveformDocumentId {WaveformDocumentId} already processed", waveformDocument.WaveformDocumentId);

        return ProcessPendingWaveformResult.FailFileUploaded();
    }

    private Task<Data.Entities.PAD> GetPad(WaveformDocument document, CancellationToken cancellationToken)
        => _mediator.Send(new GetPadByMemberPlanId(document.MemberPlanId, document.DateOfExam), cancellationToken);

    private async Task<ProcessPendingWaveformResult> ProcessFile(ProcessPendingWaveform request, CancellationToken cancellationToken)
    {
        var now = _applicationTime.UtcNow();

        var waveformDocument = _mapper.Map<WaveformDocument>(request);
        waveformDocument.CreatedDateTime = now;

        if (waveformDocument.MemberPlanId <= 0)
        {
            // ANC-2761 - We are receiving PDF files with negative member plan ids that indicate they are
            // test pdf files.  When we get these PDF files we should move them to the Ignored directory
            // for later investigation.
            _logger.LogInformation("For PDF {Filename} we are moving to the Ignored directory because of MemberPlanId:{MemberPlanId}",
                request.Filename, waveformDocument.MemberPlanId);

            return ProcessPendingWaveformResult.Ignore();
        }

        var pad = await GetPad(waveformDocument, cancellationToken);
        if (pad == null)
        {
            // It's expected that we can receive waveform PDFs before an evaluation is finalized.
            // Most evaluations are finalized the same day or the day after the DOS, but it can sometimes
            // be even more than that.

            _logger.LogDebug("Unable to find a PAD record to associate with file");

            return ProcessPendingWaveformResult.Fail();
        }

        waveformDocument = await _mediator.Send(new CreateWaveformDocument(waveformDocument), cancellationToken);

        _logger.LogInformation("Associated WaveformDocumentId {WaveformDocumentId} with PadId {PadId}, EvaluationId {EvaluationId}",
            waveformDocument.WaveformDocumentId, pad.PADId, pad.EvaluationId);

        Task SaveStatus(PADStatusCode statusCode)
        {
            return _mediator.Send(new CreatePadStatus { PadId = pad.PADId, StatusCode = statusCode }, cancellationToken);
        }

        await SaveStatus(PADStatusCode.WaveformDocumentDownloaded);
        var fileUpload = await _mediator.Send(new UploadWaveformDocument(pad.EvaluationId!.Value, request.Filename, request.FilePath), cancellationToken);
        if (!fileUpload.IsSuccess)
        {
            return ProcessPendingWaveformResult.FailFileUploaded();
        }
        await SaveStatus(PADStatusCode.WaveformDocumentUploaded);

        _logger.LogInformation("Finished processing WaveformDocumentId {WaveformDocumentId}, for PadId {PadId}, EvaluationId {EvaluationId}",
            waveformDocument.WaveformDocumentId, pad.PADId, pad.EvaluationId);
        return ProcessPendingWaveformResult.Success(pad.ClientId);
    }
}