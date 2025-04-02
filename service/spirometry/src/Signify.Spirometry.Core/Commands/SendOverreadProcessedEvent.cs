using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using NServiceBus;
using SpiroNsb.SagaEvents;

namespace Signify.Spirometry.Core.Commands;

/// <summary>
/// Event to be used to send separate events for EvaluationSaga and ProviderPaySaga
/// based on the OverreadProcessed event
/// </summary>
public class SendOverreadProcessedEvent : IRequest
{
    public OverreadProcessedEvent OverreadProcessedBaseEvent { get; }
    public bool IsPayable { get; set; }

    /// <summary>
    /// Nsb context of the calling Nsb handler
    /// </summary>
    public IMessageHandlerContext Context { get; }

    public SendOverreadProcessedEvent(OverreadProcessedEvent overreadProcessedBaseEvent, bool isPayable, IMessageHandlerContext context)
    {
        OverreadProcessedBaseEvent = overreadProcessedBaseEvent;
        IsPayable = isPayable;
        Context = context;
    }
}

public class SendOverreadProcessedEventHandler : IRequestHandler<SendOverreadProcessedEvent>
{
    private readonly IMapper _mapper;

    public SendOverreadProcessedEventHandler(IMapper mapper)
    {
        _mapper = mapper;
    }

    public async Task Handle(SendOverreadProcessedEvent request, CancellationToken cancellationToken)
    {
        await request.Context.SendLocal(request.OverreadProcessedBaseEvent);
        var eventForPayment = _mapper.Map<OverreadProcessedEventForPayment>(request.OverreadProcessedBaseEvent);
        eventForPayment.IsPayable = request.IsPayable;
        await request.Context.SendLocal(eventForPayment);
    }
}
