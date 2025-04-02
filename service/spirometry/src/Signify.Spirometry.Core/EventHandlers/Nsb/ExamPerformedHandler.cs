using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Configs.Loopback;
using Signify.Spirometry.Core.Constants;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Events.Akka;
using Signify.Spirometry.Core.Models;
using Signify.Spirometry.Core.Queries;
using Signify.Spirometry.Core.Services;
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

public class ExamPerformedHandler : IHandleMessages<ExamPerformedEvent>
{
    private readonly ILogger _logger;
    private readonly IGetLoopbackConfig _loopbackConfig;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly ITransactionSupplier _transactionSupplier;
    private readonly IExamQualityService _examQualityService;
    private readonly IPublishObservability _publishObservability;
    
    public ExamPerformedHandler(ILogger<ExamPerformedHandler> logger,
        IGetLoopbackConfig loopbackConfig,
        IMediator mediator,
        IMapper mapper,
        ITransactionSupplier transactionSupplier,
        IExamQualityService examQualityService, 
        IPublishObservability publishObservability)
    {
        _logger = logger;
        _loopbackConfig = loopbackConfig;
        _mediator = mediator;
        _mapper = mapper;
        _transactionSupplier = transactionSupplier;
        _examQualityService = examQualityService;
        _publishObservability = publishObservability;
    }

    [Transaction]
    public async Task Handle(ExamPerformedEvent message, IMessageHandlerContext context)
    {
        // Validate the raw results
        var validatedResult = _mapper.Map<ExamResult>(message.Result);

        using var transaction = _transactionSupplier.BeginTransaction();

        // Save the new exam to db
        var exam = await _mediator.Send(new AddExam(message.Exam), context.CancellationToken);

        await SendStatus(StatusCode.SpirometryExamPerformed);

        // Save the exam results to db
        var spirometryExamResult = await _mediator.Send(new AddExamResults(exam.SpirometryExamId, validatedResult), context.CancellationToken);

        bool? isBillable, needsFlag, isPayable;

        var needsOverread = NeedsOverread(exam, spirometryExamResult);
        if (needsOverread)
        {
            // If an overread is required, we don't know yet whether the results will be billable
            isBillable = null;
            isPayable = null;
            needsFlag = _examQualityService.NeedsFlag(spirometryExamResult);
        }
        else
        {
            // Check if the POC results are billable and payable
            isBillable = await IsBillable(message, spirometryExamResult, context.CancellationToken);
            isPayable = await IsPayable(message, spirometryExamResult, context.CancellationToken);

            // Flags are only ever applicable when an overread is required
            needsFlag = false;

            // Only publish results to Kafka now if an overread is not required.
            // If an overread is required, the results will be published after the
            // overread has been processed.
            await PublishResults(message, spirometryExamResult, isBillable!.Value, context.CancellationToken);
            await SendStatus(StatusCode.ResultsReceived);
        }

        var evaluationProcessedEvent = new EvaluationProcessedEvent
        {
            EvaluationId = exam.EvaluationId,
            CreatedDateTime = spirometryExamResult.CreatedDateTime,
            SpirometryExamId = exam.SpirometryExamId,
            IsPerformed = true,
            NeedsOverread = needsOverread,
            NeedsFlag = needsFlag,
            IsBillable = isBillable
        };
        await _mediator.Send(new SendEvaluationProcessedEvent(evaluationProcessedEvent, isPayable, context), context.CancellationToken);
        _logger.LogInformation(
            "Finished handling evaluation where an exam was performed, for EvaluationId={EvaluationId}, SpirometryExamId={SpirometryExamId}, IsBillable={IsBillable}, NeedsOverread={NeedsOverread}, NeedsFlag={NeedsFlag}",
            exam.EvaluationId, exam.SpirometryExamId, isBillable, needsOverread, needsFlag);

        await transaction.CommitAsync(context.CancellationToken);

        Task SendStatus(StatusCode statusCode)
        {
            return _mediator.Send(new ExamStatusEvent
            {
                EventId = message.EventId,
                Exam = exam,
                StatusCode = statusCode,
                StatusDateTime = exam.EvaluationReceivedDateTime
            }, context.CancellationToken);
        }
        
        PublishObservability(exam, Observability.Evaluation.EvaluationPerformedEvent);
    }

    private Task PublishResults(ExamPerformedEvent message, SpirometryExamResult spirometryExamResult, bool isBillable, CancellationToken token)
    {
        var resultsToPublish = _mapper.Map<ResultsReceived>(spirometryExamResult);
        _mapper.Map(message.Exam, resultsToPublish);

        resultsToPublish.IsBillable = isBillable;

        return _mediator.Send(new PublishResults(resultsToPublish, message.EventId), token);
    }

    private async Task<bool> IsBillable(ExamPerformedEvent message, SpirometryExamResult spirometryExamResult, CancellationToken token)
        => (await _mediator.Send(new QueryBillability(message.EventId, message.Exam.EvaluationId, spirometryExamResult), token)).IsBillable;

    private async Task<bool> IsPayable(ExamPerformedEvent message, SpirometryExamResult spirometryExamResult, CancellationToken token)
        => (await _mediator.Send(new QueryPayable(message.EventId, message.Exam.EvaluationId, spirometryExamResult), token)).IsPayable;

    private bool NeedsOverread(SpirometryExam exam, SpirometryExamResult spirometryExamResult)
    {
        return _loopbackConfig.ShouldProcessOverreads &&
               _loopbackConfig.IsVersionEnabled(exam.FormVersionId) &&
               _examQualityService.NeedsOverread(spirometryExamResult);
    }
    
    private void PublishObservability(SpirometryExam exam, string eventType)
    {
        var observabilityPerformedEvent = new ObservabilityEvent
        {
            EvaluationId = exam.EvaluationId,
            EventType = eventType,
            EventValue = new Dictionary<string, object>
            {
                { Observability.EventParams.EvaluationId, exam.EvaluationId },
                { Observability.EventParams.CreatedDateTime, ((DateTimeOffset)exam.CreatedDateTime).ToUnixTimeSeconds() }
            }
        };

        _publishObservability.RegisterEvent(observabilityPerformedEvent, true);
    }
}