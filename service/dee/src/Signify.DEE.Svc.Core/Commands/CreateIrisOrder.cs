using System.Diagnostics.CodeAnalysis;
using AutoMapper;
using Iris.Public.Order;
using Iris.Public.Types.Models.V2_3_1;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.DEE.Svc.Core.Messages.Models;
using System.Threading;
using System.Threading.Tasks;
using Signify.DEE.Svc.Core.Constants;
using Signify.DEE.Svc.Core.Messages.Commands;

namespace Signify.DEE.Svc.Core.Commands;

[ExcludeFromCodeCoverage]
public class CreateIrisOrder : IRequest<bool>
{
    public OrderRequest Request { get; set; }
    public ExamModel Exam { get; set; }
    public ExamAnswersModel ExamAnswers { get; set; }
}

public class CreateIrisOrderHandler(
    ILogger<CreateIrisOrderHandler> log,
    OrderSubmissionService orderSubmissionService,
    IMapper mapper,
    IMediator mediator)
    : IRequestHandler<CreateIrisOrder, bool>
{
    public async Task<bool> Handle(CreateIrisOrder createIrisOrder, CancellationToken cancellationToken)
    {
        log.LogInformation("Received Create Iris Order request for: {examLocalId}", createIrisOrder.Exam.ExamLocalId);
        createIrisOrder.Request = mapper.Map<OrderRequest>(createIrisOrder);
        await orderSubmissionService.SubmitRequest(createIrisOrder.Request);
        log.LogInformation("Published Create Iris Order request for: {examLocalId}", createIrisOrder.Exam.ExamLocalId);
        await mediator.Send(new RegisterObservabilityEvent { EvaluationId = createIrisOrder.Exam.EvaluationId, EventType = Observability.DeeStatusEvents.OrderSentToIrisEvent }, cancellationToken);

        return true;
    }
}