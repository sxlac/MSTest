using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Refit;
using Signify.PAD.Svc.Core.ApiClient.Requests;
using Signify.PAD.Svc.Core.ApiClient;
using Signify.PAD.Svc.Core.Configs;
using Signify.PAD.Svc.Core.Constants;
using Signify.PAD.Svc.Core.Models;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Threading;

namespace Signify.PAD.Svc.Core.Commands;

public class UploadPendingWaveformResult
{
    public bool IsSuccess { get; set; }
    
    public static UploadPendingWaveformResult Success() => new() { IsSuccess = true};
    public static UploadPendingWaveformResult Fail() => new() { IsSuccess = false};
}

public class UploadWaveformDocument : IRequest<UploadPendingWaveformResult>
{
    public int EvaluationId { get; }
    public string Filename { get; }
    public string FilePath { get; }

    public UploadWaveformDocument(int evaluationId, string filename, string filePath)
    {
        EvaluationId = evaluationId;
        Filename = filename;
        FilePath = filePath;
    }
}

public class UploadWaveformDocumentHandler : IRequestHandler<UploadWaveformDocument, UploadPendingWaveformResult>
{
    private readonly OktaConfig _oktaConfig;
    private readonly IEvaluationApi _evaluationApi;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;

    public UploadWaveformDocumentHandler(ILogger<UploadWaveformDocumentHandler> logger,
        OktaConfig oktaConfig,
        IEvaluationApi evaluationApi,
        IFileSystem fileSystem)
    {
        _logger = logger;
        _oktaConfig = oktaConfig;
        _evaluationApi = evaluationApi;
        _fileSystem = fileSystem;
    }

    [Transaction]
    public async Task<UploadPendingWaveformResult> Handle(UploadWaveformDocument request, CancellationToken cancellationToken)
    {
        var existsResponse = await _evaluationApi.GetEvaluationDocumentDetails(request.EvaluationId, Application.WaveformDocumentType);
        if (ContainsPadWaveform(existsResponse))
        {
            _logger.LogInformation("Evaluation API already has a Waveform document for EvaluationId {EvaluationId}, not uploading file", request.EvaluationId);
            return UploadPendingWaveformResult.Fail();
        }

        _logger.LogInformation("Uploading file to the Evaluation API for EvaluationId {EvaluationId}", request.EvaluationId);
        
        await using var filePdf = _fileSystem.FileStream.New(request.FilePath, FileMode.Open, FileAccess.Read);
        using var ms = new MemoryStream();
        await filePdf.CopyToAsync(ms, cancellationToken);
        var byteArrayPart = new ByteArrayPart(ms.ToArray(), request.Filename);
            
        var response = await _evaluationApi.CreateEvaluationDocument(request.EvaluationId, byteArrayPart, new EvaluationRequest
        {
            ApplicationId = Application.ApplicationId,
            DocumentType = Application.WaveformDocumentType,
            UserName = _oktaConfig.ClientId
        });

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Successfully uploaded file to the Evaluation API for EvaluationId {EvaluationId}", request.EvaluationId);
            return UploadPendingWaveformResult.Success();
        }

        _logger.LogWarning("Failed to upload file to the Evaluation API for EvaluationId {EvaluationId}", request.EvaluationId);
        throw response.Error;
    }

    private static bool ContainsPadWaveform(IApiResponse<IList<EvaluationDocumentModel>> response)
    {
        return response is {StatusCode: HttpStatusCode.OK, Content: { }} 
               && response.Content.Any(doc => doc.DocumentType == Application.WaveformDocumentType);
    }
}