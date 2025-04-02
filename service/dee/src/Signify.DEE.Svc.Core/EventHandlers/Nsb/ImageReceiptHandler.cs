using Iris.Public.Types.Models.Public._2._3._1;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.DEE.Svc.Core.Constants;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Infrastructure;
using Signify.DEE.Svc.Core.Messages.Commands;
using Signify.DEE.Svc.Core.Messages.Queries;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.EventHandlers.Nsb;

public class ImageReceiptHandler(
    ILogger<ImageReceiptHandler> logger,
    IMediator mediator,
    ITransactionSupplier transactionSupplier,
    IApplicationTime applicationTime)
    : IHandleMessages<ImageReceipt>
{
    [Transaction]
    public async Task Handle(ImageReceipt message, IMessageHandlerContext context)
    {
        logger.LogInformation("Received ImageReceipt for Image Local ID: {ImageLocalId}", message.ImageLocalId);
        if (!message.Success)
        {
            logger.LogWarning("ImageReceipt failed for Image local ID: {ImageLocalId} and Iris image Id {IrisImageId} because {ErrorMessage}", message.ImageLocalId, message.IrisImageId, message.ErrorMessage);
            return;
        }

        var imgLocalId = message.ImageLocalId;
        var img = await mediator.Send(new GetExamImageByLocalId { LocalId = imgLocalId }, context.CancellationToken);
        if (img is null)
        {
            // If a transaction is aborted, Iris will still receive the image and may confirm receipt of it
            // but we never kept that exam in the DB.
            logger.LogInformation("ImageReceipt could not find exam by local ID: {ImageLocalId}", message.ImageLocalId);
            return;
        }
        var transaction = transactionSupplier.BeginTransaction();

        await mediator.Send(new CreateStatus(img.ExamId, ExamStatusCode.IRISImageReceived.Name, applicationTime.UtcNow()), context.CancellationToken);

        logger.LogInformation("Finished recording ImageReceipt for Image Local ID: {ImageLocalId}", message.ImageLocalId);

        await transaction.CommitAsync(context.CancellationToken);

        var exam = await mediator.Send(new GetExamByImageLocalId() { ImageLocalId = imgLocalId }, context.CancellationToken);
        await mediator.Send(new RegisterObservabilityEvent { EvaluationId = (long)exam.EvaluationId, EventType = Observability.DeeStatusEvents.IrisImageReceivedEvent }, context.CancellationToken);
    }
}