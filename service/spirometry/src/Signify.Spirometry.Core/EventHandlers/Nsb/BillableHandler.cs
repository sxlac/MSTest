using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using System;
using System.Threading;
using System.Threading.Tasks;
using Signify.Dps.Observability.Library.Services;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Queries;
using SpiroNsbEvents;

using ExamStatusEvent = Signify.Spirometry.Core.Events.ExamStatusEvent;
using StatusCode = Signify.Spirometry.Core.Models.StatusCode;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers;

/// <summary>
/// Coordinates the processing of a <see cref="BillableEvent"/>
/// </summary>
public class BillableHandler : IHandleMessages<BillableEvent>
{
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;
    private readonly ITransactionSupplier _transactionSupplier;
    private readonly IPublishObservability _publishObservability;

    public BillableHandler(ILogger<BillableHandler> logger,
        IMapper mapper,
        IMediator mediator,
        ITransactionSupplier transactionSupplier, IPublishObservability publishObservability)
    {
        _logger = logger;
        _mapper = mapper;
        _mediator = mediator;
        _transactionSupplier = transactionSupplier;
        _publishObservability = publishObservability;
    }

    [Transaction]
    public async Task Handle(BillableEvent message, IMessageHandlerContext context)
    {
        using var transaction = _transactionSupplier.BeginTransaction();

        await SendBillableEventReceivedStatus(message, context.CancellationToken);

        var billRequestSent = (await _mediator.Send(new QueryBillRequestSent(message.EvaluationId), context.CancellationToken)).Entity;
        if (billRequestSent == null)
        {
            _logger.LogInformation("This evaluation has not been billed for yet, sending command to create billing request, for EvaluationId={EvaluationId} with EventId={EventId}",
                message.EvaluationId, message.EventId);

            billRequestSent = await _mediator.Send(_mapper.Map<CreateBill>(message), context.CancellationToken);

            await SendBillRequestSentStatus(message, billRequestSent, context.CancellationToken);

            _logger.LogInformation("BillRequestSent event raised, for EvaluationId={EvaluationId} with EventId={EventId}",
                message.EvaluationId, message.EventId);
        }

        await transaction.CommitAsync(context.CancellationToken);
        _publishObservability.Commit();
    }

    private async Task SendBillableEventReceivedStatus(BillableEvent billableEvent, CancellationToken token)
    {
        await _mediator.Send(await CreateStatusEvent(billableEvent, StatusCode.BillableEventReceived,
            billableEvent.BillableDate, token), token);
    }

    private async Task SendBillRequestSentStatus(BillableEvent billableEvent, BillRequestSent billRequestSent, CancellationToken token)
    {
        await _mediator.Send(await CreateStatusEvent(billableEvent, StatusCode.BillRequestSent,
            billRequestSent.CreatedDateTime, token), token);
    }

    private async Task<ExamStatusEvent> CreateStatusEvent(BillableEvent billableEvent, StatusCode statusCode, DateTime statusDateTime, CancellationToken token)
    {
        var exam = await _mediator.Send(new QuerySpirometryExam(billableEvent.EvaluationId), token);

        return new ExamStatusEvent
        {
            EventId = billableEvent.EventId,
            Exam = exam,
            StatusCode = statusCode,
            StatusDateTime = statusDateTime
        };
    }
}