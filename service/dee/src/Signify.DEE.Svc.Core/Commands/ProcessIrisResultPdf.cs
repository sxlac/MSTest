using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Exceptions;
using Signify.DEE.Svc.Core.Infrastructure;
using Signify.DEE.Svc.Core.Messages.Commands;
using Signify.DEE.Svc.Core.Messages.Queries;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.Commands;

[ExcludeFromCodeCoverage]
public class ProcessIrisResultPdf : ICommand
{
    public long EvaluationId { get; set; }
    public DateTimeOffset CreatedDateTime { get; set; }
    public string PdfData { get; set; }
    //Could be that localId and examId is the same.
    public int ExamId { get; set; }

    public ProcessIrisResultPdf(long evaluationId, string pdfData, int examId)
    {
        EvaluationId = evaluationId;
        PdfData = pdfData;
        ExamId = examId;
    }

    public ProcessIrisResultPdf()
    {
    }
}

public class ProcessIrisResultPdfHandler(
    ILogger<ProcessIrisResultPdfHandler> logger,
    IMediator mediator,
    ITransactionSupplier transactionSupplier,
    IApplicationTime applicationTime)
    : IHandleMessages<ProcessIrisResultPdf>
{
    [Transaction]
    public async Task Handle(ProcessIrisResultPdf command, IMessageHandlerContext context)
    {
        var pdfDataBytes = await mediator.Send(new GetPdfDataFromString { EvaluationId = command.EvaluationId, PdfData = command.PdfData }, context.CancellationToken);

        if (pdfDataBytes is not null)
        {
            //Upload pdf data to Evaluation API
            logger.LogInformation("Valid Pdf received for EvaluationId={EvaluationId}", command.EvaluationId);
            await mediator.Send(new CreateExamResultPdf(command.ExamId, pdfDataBytes), context.CancellationToken);

            //DB Transaction
            using var transaction = transactionSupplier.BeginTransaction();
            await mediator.Send(new CreateStatus(command.ExamId, ExamStatusCode.PDFDataDownloaded.Name, applicationTime.UtcNow()), context.CancellationToken);
            await transaction.CommitAsync(context.CancellationToken);
        }
        else
        {
            logger.LogInformation("Invalid Pdf received for EvaluationId={EvaluationId}, throwing exception for Invalid Pdf", command.EvaluationId);
            throw new InvalidPdfException(command.EvaluationId);
        }
    }
}