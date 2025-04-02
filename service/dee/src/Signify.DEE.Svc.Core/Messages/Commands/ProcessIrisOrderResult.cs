using AutoMapper;
using Iris.Public.Types.Models.V2_3_1;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Infrastructure;
using Signify.DEE.Svc.Core.Messages.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.Messages.Commands;

public class ProcessIrisOrderResult : IRequest<Unit>
{
    public OrderResult OrderResult { get; set; }
    public ExamModel Exam { get; set; }
}

public class ProcessIrisOrderResultHandler(
    ILogger<ProcessIrisOrderResultHandler> logger,
    IMapper mapper,
    IMediator mediator,
    IApplicationTime applicationTime)
    : IRequestHandler<ProcessIrisOrderResult, Unit>
{
    [Transaction]
    public async Task<Unit> Handle(ProcessIrisOrderResult request, CancellationToken cancellationToken)
    {

        logger.LogDebug("ExamId:{ExamId} -- handler ProcessIrisOrderResult started", request.Exam.ExamId);

        //Initial status needed before result ingestion starts.
        //Create Status IRIS exam created.
        await mediator.Send(new CreateStatus(request.Exam.ExamId, ExamStatusCode.IRISExamCreated.Name, applicationTime.UtcNow()), cancellationToken).ConfigureAwait(false);

        //Create Status IRIS exam interpreted.
        await mediator.Send(new CreateStatus(request.Exam.ExamId, ExamStatusCode.IRISInterpreted.Name, applicationTime.UtcNow()), cancellationToken).ConfigureAwait(false);

        // Map Iris Order Result data to CreateExamResultRecord
        var examResultModel = mapper.Map<ExamResultModel>(request);

        // Call CreateExamResultRecord Handler to save Iris Order Result Data to Db
        await mediator.Send(new CreateExamResultRecord(examResultModel), cancellationToken);

        return Unit.Value;
    }
}