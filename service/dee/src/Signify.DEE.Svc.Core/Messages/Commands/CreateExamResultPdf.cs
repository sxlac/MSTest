using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Refit;
using Signify.DEE.Svc.Core.ApiClient;
using Signify.DEE.Svc.Core.Configs;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Infrastructure;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.Messages.Commands;

public class CreateExamResultPdf(int examId, byte[] data) : IRequest<Unit>
{
    public int ExamId { get; set; } = examId;
    public byte[] Data { get; set; } = data;

    public override string ToString()
        => $"{nameof(ExamId)}: {ExamId}, {nameof(Data)}: {Data}";
}

public class CreateResultPdfCommandHandler(
    ILogger<CreateResultPdfCommandHandler> log,
    IEvaluationApi evaluationApi,
    IrisDocumentInfoConfig config,
    DataContext context,
    IApplicationTime applicationTime)
    : IRequestHandler<CreateExamResultPdf, Unit>
{
    [Trace]
    public async Task<Unit> Handle(CreateExamResultPdf request, CancellationToken cancellationToken)
    {
        var exam = context.Exams.FirstOrDefault(exam => exam.ExamId == request.ExamId);

        if (exam?.EvaluationId == null)
        {
            if (exam == null)
                throw new InvalidDataException($"ExamId: {request.ExamId} -- Exam record not found in DEE datastore");

            throw new ArgumentNullException($"ExamId: {exam.ExamId} -- No EvaluationId found on DEE record in DEE datastore");
        }

        var evalDocs = await evaluationApi.GetEvaluationDocument(exam.EvaluationId.Value).ConfigureAwait(false);
        if (evalDocs?.Content != null && evalDocs.Content.Any(d => d.DocumentType == "DeeResult"))
        {
            log.LogInformation("ExamId: {ExamId}, EvaluationId: {EvaluationId} -- DeeResult Document already exists", request.ExamId, exam.EvaluationId);
            return Unit.Value;
        }

        var stream = new MemoryStream(request.Data);
        var part = new StreamPart(stream, $"{applicationTime.UtcNow().Ticks}.pdf", "application/pdf");
        log.LogDebug("ExamId: {ExamId}, EvaluationId: {EvaluationId} -- Send to EvaluationApi for exam pdf create", exam.ExamId, exam.EvaluationId);

        var createEvaluationDocumentRequest = new CreateEvaluationDocumentRequest
        {
            ApplicationId = config.ApplicationId,
            DocumentType = config.DocumentType,
            UserName = config.UserName
        };
        var response = await evaluationApi.CreateEvaluationDocument(exam.EvaluationId.Value, part, createEvaluationDocumentRequest);
        
        if (response.IsSuccessStatusCode)
        {
            log.LogDebug("Exam {ExamId} for Evaluation {EvaluationId} completed download for pdf @ path: {FilePath}", exam.ExamId, response.Content!.EvaluationId, response.Content.FilePath);
            return Unit.Value;
        }

        log.LogWarning("Failed to upload file to the Evaluation API for EvaluationId {EvaluationId}", exam.EvaluationId);
        throw response.Error;

    }
}