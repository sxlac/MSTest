using MediatR;
using Microsoft.Extensions.Logging;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.EventHandlers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.Messages.Commands;

public class CreateOrUpdatePdfToClient : IRequest<PDFToClient>
{
    public int PDFDeliverId { get; set; }
    public string EventId { get; set; }
    public long EvaluationId { get; set; }
    public DateTimeOffset DeliveryDateTime { get; set; }
    public DateTimeOffset DeliveryCreatedDateTime { get; set; }
    public long BatchId { get; set; }
    public string BatchName { get; set; }
    public int ExamId { get; set; }
}

public class CreateOrUpdatePdfToClientHandler(ILogger<PdfDeliveredHandler> logger, DataContext context)
    : IRequestHandler<CreateOrUpdatePdfToClient, PDFToClient>
{
    public async Task<PDFToClient> Handle(CreateOrUpdatePdfToClient request, CancellationToken cancellationToken)
    {
        var pdfToClient = new PDFToClient
        {
            EventId = request.EventId,
            EvaluationId = request.EvaluationId,
            DeliveryDateTime = request.DeliveryDateTime,
            DeliveryCreatedDateTime = request.DeliveryCreatedDateTime,
            BatchId = request.BatchId,
            BatchName = request.BatchName,
            ExamId = request.ExamId,
            PDFDeliverId = request.PDFDeliverId,
            CreatedDateTime = request.DeliveryCreatedDateTime
        };

        if (request.PDFDeliverId == 0)
        {
            //Create new
            var newDee = await context.PDFToClient.AddAsync(pdfToClient, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return newDee.Entity;
        }
        //update

        logger.LogDebug("Updating PDFToClient PDFDeliverId:{PDFDeliverId}", pdfToClient.PDFDeliverId);
        var updateDee = context.PDFToClient.Update(pdfToClient);
        //update DEE status
        await context.SaveChangesAsync(cancellationToken);
        return updateDee.Entity;
    }
}