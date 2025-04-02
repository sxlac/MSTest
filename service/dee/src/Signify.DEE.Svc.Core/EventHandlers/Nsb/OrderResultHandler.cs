using AutoMapper;
using Iris.Public.Types.Models.V2_3_1;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.DEE.Core.Messages.Queries;
using Signify.DEE.Svc.Core.Commands;
using Signify.DEE.Svc.Core.Constants;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Exceptions;
using Signify.DEE.Svc.Core.Messages.Commands;
using Signify.DEE.Svc.Core.Messages.Models;
using Signify.DEE.Svc.Core.Messages.Queries;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.EventHandlers.Nsb;

public class OrderResultHandler(
    ILogger<OrderResultHandler> logger,
    IMediator mediator,
    IMapper mapper,
    ITransactionSupplier transactionSupplier)
    : IHandleMessages<OrderResult>
{
    [Transaction]
    public async Task Handle(OrderResult message, IMessageHandlerContext context)
    {
        var examEntity = await mediator.Send(new GetExamByLocalId { LocalId = message.Order.LocalId }, context.CancellationToken);
        if (examEntity is null || examEntity.EvaluationId == 0)
        {
            logger.LogInformation("OrderResultHandler - Order LocalId: {LocalId} -- No Exam record found in DEE datastore", message.Order.LocalId);
            throw new UnmatchedOrderException(message.Order.LocalId, message.Order.PatientOrderID);
        }
        var exam = mapper.Map<ExamModel>(examEntity);

        using var transaction = transactionSupplier.BeginTransaction();
        //Add ExamResults
        await mediator.Send(new ProcessIrisOrderResult
        {
            OrderResult = message,
            Exam = exam
        }, context.CancellationToken);

        //Update Images for Laterality, Gradable and Non-gradable reason if not gradable.
        await mediator.Send(new ProcessIrisImagesResult
        {
            OrderResult = message,
            Exam = exam
        }, context.CancellationToken);
        await transaction.CommitAsync(context.CancellationToken);

        await mediator.Send(new RegisterObservabilityEvent { EvaluationId = exam.EvaluationId, EventType = Observability.DeeStatusEvents.IrisResultsReceivedEvent }, context.CancellationToken);

        //Publish order results
        await context.SendLocal(new PublishIrisOrderResult
        {
            Exam = exam
        });

        //Process PDFs
        await context.SendLocal(new ProcessIrisResultPdf
        {
            ExamId = exam.ExamId,
            EvaluationId = exam.EvaluationId,
            PdfData = message.ResultsDocument!.Content,
        });

        //Add Bill Statuses and send Bill if PDF is already received
        await context.SendLocal(new DetermineBillabityOfResult
        {
            Exam = exam,
            Gradings = message.Gradings,
            ImageDetails = message.ImageDetails
        });

        //Release the cdi hold if it exists        
        var hold = await mediator.Send(new GetHold() { EvaluationId = exam.EvaluationId }, context.CancellationToken);
        if (hold is not null)
        {
            await context.SendLocal(new ReleaseHold(hold));
        }
    }
}