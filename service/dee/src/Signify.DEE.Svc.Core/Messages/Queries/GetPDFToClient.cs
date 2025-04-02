using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Messages.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.Messages.Queries;

public class GetPdfToClient(int examId, long? evaluationId) : IRequest<PdfToClientModel>
{
    public int? ExamId { get; set; } = examId;
    public long? EvaluationId { get; set; } = evaluationId;
}

public class GetPdfToClientHandler(ILogger<GetPdfToClientHandler> log, DataContext context, IMapper mapper)
    : IRequestHandler<GetPdfToClient, PdfToClientModel>
{
    [Trace]
    public Task<PdfToClientModel> Handle(GetPdfToClient request, CancellationToken cancellationToken)
    {
        log.LogDebug("{Request} -- PDFToClient lookup", request);

        var pdfToClient = context.PDFToClient.FirstOrDefault(e => e.ExamId == request.ExamId && e.EvaluationId == request.EvaluationId);

        if (pdfToClient != null)
        {
            log.LogDebug("ExamId: {ExamId}, EvaluationId: {EvaluationId} -- pDFToClient record found",
                pdfToClient.ExamId, pdfToClient.EvaluationId);
            return Task.FromResult(mapper.Map<PdfToClientModel>(pdfToClient));
        }

        log.LogDebug("ExamId: {ExamId},-- pDFToClient record not found", request.ExamId);
        return Task.FromResult(new PdfToClientModel());
    }
}