using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Configs.Loopback;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Events;
using Signify.Spirometry.Core.Events.Akka;
using Signify.Spirometry.Core.Exceptions;
using Signify.Spirometry.Core.Infrastructure;
using Signify.Spirometry.Core.Queries;
using Signify.Spirometry.Core.Services;
using SpiroNsb.SagaEvents;
using System;
using System.Threading;
using System.Threading.Tasks;
using StatusCode = Signify.Spirometry.Core.Models.StatusCode;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace SpiroNsb.SagaCommands;

/// <summary>
/// Command to process a spirometry diagnostic overread
/// </summary>
public class ProcessOverread : ISagaCommand
{
    /// <inheritdoc />
    public long EvaluationId { get; set; }

    /// <summary>
    /// PK of the OverreadResult in db
    /// </summary>
    public int OverreadResultId { get; set; }

    public ProcessOverread(long evaluationId, int overreadResultId)
    {
        EvaluationId = evaluationId;
        OverreadResultId = overreadResultId;
    }
}

public class ProcessOverreadHandler : IHandleMessages<ProcessOverread>
{
    private readonly ILogger _logger;
    private readonly IApplicationTime _applicationTime;
    private readonly IGetLoopbackConfig _config;
    private readonly ITransactionSupplier _transactionSupplier;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly IExamQualityService _examQualityService;

#pragma warning disable S107
    public ProcessOverreadHandler(ILogger<ProcessOverreadHandler> logger,
        IApplicationTime applicationTime,
        IGetLoopbackConfig config,
        ITransactionSupplier transactionSupplier,
        IMediator mediator,
        IMapper mapper,
        IExamQualityService examQualityService)
#pragma warning restore S107
    {
        _logger = logger;
        _applicationTime = applicationTime;
        _config = config;
        _transactionSupplier = transactionSupplier;
        _mediator = mediator;
        _mapper = mapper;
        _examQualityService = examQualityService;
    }

    [Transaction]
    public async Task Handle(ProcessOverread message, IMessageHandlerContext context)
    {
        _logger.LogInformation("Received ProcessOverread request for EvaluationId={EvaluationId}, with OverreadResultId={OverreadResultId}",
            message.EvaluationId, message.OverreadResultId);

        if (!_config.ShouldProcessOverreads)
        {
            _logger.LogInformation(
                "Processing overreads is disabled, not processing for EvaluationId={EvaluationId}, with OverreadResultId={OverreadResultId}",
                message.EvaluationId, message.OverreadResultId);

            // Throw an exception instead of just `return` so the message isn't permanently lost and
            // can be replayed from the error queue if needed.
            throw new FeatureDisabledException(message.EvaluationId, nameof(_config.ShouldProcessOverreads));
        }

        var (exam, results, overread) = await GetData(message, context.CancellationToken);

        if (!ShouldProcessOverread(exam, results))
            return;

        using var transaction = _transactionSupplier.BeginTransaction();

        var (overreadProcessed, isPayable) = await ProcessOverread(exam, results, overread, context.CancellationToken);
        await _mediator.Send(new SendOverreadProcessedEvent(overreadProcessed, isPayable, context), context.CancellationToken);

        await transaction.CommitAsync(context.CancellationToken);

        _logger.LogInformation("Updated exam results from overread, for EvaluationId={EvaluationId}, IsBillable={IsBillable}, IsPayable={IsPayable}, NeedsFlag={NeedsFlag}",
            exam.EvaluationId, overreadProcessed.IsBillable, isPayable, overreadProcessed.NeedsFlag);
    }

    [Trace]
    private async Task<(SpirometryExam exam, SpirometryExamResult results, OverreadResult overread)> GetData(ISagaCommand message, CancellationToken token)
    {
        var exam = await _mediator.Send(new QuerySpirometryExam(message.EvaluationId)
        {
            IncludeResults = true // join, to save a db hit
        }, token);

        var overread = await _mediator.Send(new QueryOverreadResult(exam.AppointmentId), token);

        return (exam, exam.SpirometryExamResult, overread);
    }

    private bool ShouldProcessOverread(SpirometryExam exam, SpirometryExamResult result)
    {
        if (result.OverreadFev1FvcRatio.HasValue)
        {
            _logger.LogWarning("For EvaluationId={EvaluationId}, overread has already been processed, nothing to do", exam.EvaluationId);
            return false;
        }

        var needsOverread = _examQualityService.NeedsOverread(result);

        if (!needsOverread)
            _logger.LogWarning("For EvaluationId={EvaluationId}, the POC results do not require an overread, nothing to do", exam.EvaluationId);

        return needsOverread;
    }

    [Trace]
    private async Task<(OverreadProcessedEvent, bool isPayable)> ProcessOverread(SpirometryExam exam, SpirometryExamResult results, OverreadResult overread, CancellationToken token)
    {
        results.NormalityIndicatorId = overread.NormalityIndicatorId;
        results.OverreadFev1FvcRatio = overread.Fev1FvcRatio;

        await _mediator.Send(new UpdateExamResults(results), token);

        var isBillable = (await _mediator.Send(new QueryBillability(Guid.Empty, exam.EvaluationId, results), token))
            .IsBillable;
        var isPayable = (await _mediator.Send(new QueryPayable(Guid.Empty, exam.EvaluationId, results), token)).IsPayable;

        await SendStatus(StatusCode.OverreadProcessed, _applicationTime.UtcNow());
        await PublishResults(exam, results, isBillable, overread.ReceivedDateTime, token);
        await SendStatus(StatusCode.ResultsReceived, overread.ReceivedDateTime);

        return (new OverreadProcessedEvent
        {
            EvaluationId = exam.EvaluationId,
            CreatedDateTime = _applicationTime.UtcNow(),
            IsBillable = isBillable,
            NeedsFlag = _examQualityService.NeedsFlag(results)!.Value
        }, isPayable);

        Task SendStatus(StatusCode statusCode, DateTime statusDateTime)
        {
            return _mediator.Send(new ExamStatusEvent
            {
                EventId = Guid.Empty,
                Exam = exam,
                StatusCode = statusCode,
                StatusDateTime = statusDateTime
            }, token);
        }
    }

    private Task PublishResults(SpirometryExam exam, SpirometryExamResult spirometryExamResult, bool isBillable, DateTime overreadReceivedDate, CancellationToken token)
    {
        var resultsToPublish = _mapper.Map<ResultsReceived>(spirometryExamResult);
        _mapper.Map(exam, resultsToPublish);

        resultsToPublish.IsBillable = isBillable;
        resultsToPublish.ReceivedDate = overreadReceivedDate;

        return _mediator.Send(new PublishResults(resultsToPublish, Guid.Empty), token);
    }
}