using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using NServiceBus;
using SpiroNsb.SagaEvents;

namespace Signify.Spirometry.Core.Commands;

/// <summary>
/// Event to be used to send separate events for EvaluationSaga and ProviderPaySaga
/// based on the EvaluationProcessed event
/// </summary>
public class SendEvaluationProcessedEvent : IRequest
{
    public EvaluationProcessedEvent EvaluationProcessedEvent { get; }
    public bool? IsPayable { get; set; }

    /// <summary>
    /// Nsb context of the calling Nsb handler
    /// </summary>
    public IMessageHandlerContext Context { get; }

    public SendEvaluationProcessedEvent(EvaluationProcessedEvent evaluationProcessedEvent, bool? isPayable, IMessageHandlerContext context)
    {
        EvaluationProcessedEvent = evaluationProcessedEvent;
        IsPayable = isPayable;
        Context = context;
    }
}

public class SendEvaluationProcessedEventHandler : IRequestHandler<SendEvaluationProcessedEvent>
{
    private readonly IMapper _mapper;

    public SendEvaluationProcessedEventHandler(IMapper mapper)
    {
        _mapper = mapper;
    }

    public async Task Handle(SendEvaluationProcessedEvent request, CancellationToken cancellationToken)
    {
        await request.Context.SendLocal(request.EvaluationProcessedEvent);
        var eventForPayment = _mapper.Map<EvaluationProcessedEventForPayment>(request.EvaluationProcessedEvent);
        eventForPayment.IsPayable = request.IsPayable;
        await request.Context.SendLocal(eventForPayment);
    }
}