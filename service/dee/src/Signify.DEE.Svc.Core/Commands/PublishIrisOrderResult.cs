using System.Diagnostics.CodeAnalysis;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.DEE.Messages.Status;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Infrastructure;
using Signify.DEE.Svc.Core.Messages.Commands;
using Signify.DEE.Svc.Core.Messages.Models;
using Signify.DEE.Svc.Core.Messages.Queries;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.Commands;

[ExcludeFromCodeCoverage]
public class PublishIrisOrderResult : ICommand
{
    public ExamModel Exam { get; set; }
}

public class PublishIrisOrderResultHandler(
    ILogger<PublishIrisOrderResultHandler> logger,
    IMapper mapper,
    IMediator mediator,
    ITransactionSupplier transactionSupplier,
    IApplicationTime applicationTime)
    : IHandleMessages<PublishIrisOrderResult>
{
    [Transaction]
    public async Task Handle(PublishIrisOrderResult request, IMessageHandlerContext context)
    {
        logger.LogDebug("ExamId:{ExamId} -- handler PublishIrisOrderResultHandler started", request.Exam.ExamId);

        using var transaction = transactionSupplier.BeginTransaction();

        // Add Status for IRIS Result Data Downloaded
        var createStatusResponse = await mediator.Send(new CreateStatus(request.Exam.ExamId, ExamStatusCode.ResultDataDownloaded.Name, applicationTime.UtcNow()), context.CancellationToken);

        // Map to ResultsReceived
        var resultStatus = mapper.Map<ResultsReceived>(request.Exam);

        // Set ReceivedDateTime
        resultStatus.ReceivedDate = createStatusResponse.ExamStatus?.ReceivedDateTime ?? applicationTime.UtcNow();

        // Publish ResultStatus
        await mediator.Send(new PublishStatusUpdate(resultStatus), context.CancellationToken);

        // Retrieve Result Data.
        var results = await mediator.Send(new GetResultReceivedData(request.Exam.ExamId), context.CancellationToken);

        // Publish Result Data
        await mediator.Send(new PublishResult(results), context.CancellationToken);

        await transaction.CommitAsync(context.CancellationToken);
    }
}