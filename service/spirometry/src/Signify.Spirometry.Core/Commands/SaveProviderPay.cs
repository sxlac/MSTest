using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Events;
using Signify.Spirometry.Core.Infrastructure;
using Signify.Spirometry.Core.Queries;
using SpiroNsb.SagaEvents;

using StatusCode = Signify.Spirometry.Core.Models.StatusCode;

namespace Signify.Spirometry.Core.Commands;

public class SaveProviderPay : IRequest
{
    public Guid EventId { get; set; }
    public long EvaluationId { get; set; }
    public string PaymentId { get; set; }
    public int ExamId { get; set; }

    /// <summary>
    /// Date and time contained within the Kafka event that triggered this NSB
    /// </summary>
    public DateTimeOffset ParentEventDateTime { get; set; }

    /// <summary>
    /// Date and time the Kafka event that triggered this Nsb was received by PM
    /// </summary>
    public DateTimeOffset ParentEventReceivedDateTime { get; set; }

    public IMessageHandlerContext Context { get; set; }
}

public class SaveProviderPayHandler : IRequestHandler<SaveProviderPay>
{
    private readonly ILogger<SaveProviderPayHandler> _logger;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly IApplicationTime _applicationTime;

    public SaveProviderPayHandler(ILogger<SaveProviderPayHandler> logger, IMediator mediator, IMapper mapper, IApplicationTime applicationTime)
    {
        _logger = logger;
        _mediator = mediator;
        _mapper = mapper;
        _applicationTime = applicationTime;
    }

    [Transaction]
    public async Task Handle(SaveProviderPay request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting to handle save provider pay, for EvaluationId={EvaluationId}",
            request.EvaluationId);

        await WriteToProviderPayTable(request, cancellationToken);
        await PublishStatusEvent(request, StatusCode.ProviderPayRequestSent, cancellationToken);
        await UpdateProviderPaySaga(request);

        _logger.LogInformation("Finished handling SaveProviderPay, for EvaluationId={EvaluationId}", request.EvaluationId);
    }

    /// <summary>
    /// Invoke <see cref="ProviderPaidEvent"/> Saga event
    /// </summary>
    /// <param name="message"></param>
    private async Task UpdateProviderPaySaga(SaveProviderPay message)
    {
        await message.Context.SendLocal(new ProviderPaidEvent { EvaluationId = message.EvaluationId, CreatedDateTime = _applicationTime.UtcNow() });
    }

    /// <summary>
    /// Invoke ExamStatusEvent as a Mediator event to write to database StatusCode table with ProviderPayRequestSent and raise kafka event
    /// </summary>
    /// <param name="message"></param>
    /// <param name="statusCode"></param>
    /// <param name="cancellationToken"></param>
    private async Task PublishStatusEvent(SaveProviderPay message, StatusCode statusCode, CancellationToken cancellationToken)
    {
        var updateEvent = _mapper.Map<ExamStatusEvent>(message);
        updateEvent.StatusCode = statusCode;
        updateEvent.Exam = await _mediator.Send(new QuerySpirometryExam(message.EvaluationId), cancellationToken);
        await _mediator.Send(updateEvent, cancellationToken);
    }

    /// <summary>
    /// Write the new payment id entry to the ProviderPay table
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    [Trace]
    private async Task WriteToProviderPayTable(SaveProviderPay message, CancellationToken cancellationToken)
    {
        var providerPay = _mapper.Map<ProviderPay>(message);
        await _mediator.Send(new AddProviderPay(providerPay), cancellationToken);
    }
}