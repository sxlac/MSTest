using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Constants;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Data.Entities;
using SpiroNsb.SagaEvents;
using SpiroNsbEvents;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using StatusCode = Signify.Spirometry.Core.Models.StatusCode;
using ExamStatusEvent = Signify.Spirometry.Core.Events.ExamStatusEvent;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers;

public class ExamNotPerformedHandler : IHandleMessages<ExamNotPerformedEvent>
{
    private readonly ILogger _logger;
    private readonly ITransactionSupplier _transactionSupplier;
    private readonly IMediator _mediator;
    private readonly IPublishObservability _publishObservability;
    
    public ExamNotPerformedHandler(ILogger<ExamNotPerformedHandler> logger,
        ITransactionSupplier transactionSupplier,
        IMediator mediator,
        IPublishObservability publishObservability)
    {
        _logger = logger;
        _transactionSupplier = transactionSupplier;
        _mediator = mediator;
        _publishObservability = publishObservability;
    }

    [Transaction]
    public async Task Handle(ExamNotPerformedEvent message, IMessageHandlerContext context)
    {
        using var transaction = _transactionSupplier.BeginTransaction();

        // Save the new exam to db
        var exam = await _mediator.Send(new AddExam(message.Exam), context.CancellationToken);

        // Save the reason the exam wasn't performed to db
        var np = await _mediator.Send(new AddExamNotPerformed(exam, message.Info), context.CancellationToken);

        await SendStatus(message, exam, context.CancellationToken);

        var evaluationProcessedEvent = new EvaluationProcessedEvent
        {
            EvaluationId = exam.EvaluationId,
            SpirometryExamId = exam.SpirometryExamId,
            CreatedDateTime = np.CreatedDateTime,
            IsPerformed = false,
            NeedsOverread = false,
            NeedsFlag = false,
            IsBillable = false
        };
        await _mediator.Send(new SendEvaluationProcessedEvent(evaluationProcessedEvent, false, context), context.CancellationToken);
        _logger.LogInformation(
            "Finished handling evaluation where an exam was not performed, for EvaluationId={EvaluationId}, SpirometryExamId={SpirometryExamId}",
            exam.EvaluationId, exam.SpirometryExamId);

        await transaction.CommitAsync(context.CancellationToken);
        
        PublishObservability(exam, Observability.Evaluation.EvaluationNotPerformedEvent);
    }

    private Task SendStatus(ExamNotPerformedEvent message, SpirometryExam exam, CancellationToken token)
    {
        return _mediator.Send(new ExamStatusEvent
        {
            EventId = message.EventId,
            Exam = exam,
            StatusCode = StatusCode.SpirometryExamNotPerformed,
            StatusDateTime = message.Exam.EvaluationReceivedDateTime
        }, token);
    }

    private void PublishObservability(SpirometryExam exam, string eventType)
    {
        var observabilityNotPerformedEvent = new ObservabilityEvent
        {
            EvaluationId = exam.EvaluationId,
            EventType = eventType,
            EventValue = new Dictionary<string, object>
            {
                { Observability.EventParams.EvaluationId, exam.EvaluationId },
                { Observability.EventParams.CreatedDateTime, ((DateTimeOffset)exam.CreatedDateTime).ToUnixTimeSeconds() }
            }
        };

        _publishObservability.RegisterEvent(observabilityNotPerformedEvent, true);
    }
}