using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.HBA1CPOC.Messages.Events;
using Signify.HBA1CPOC.Messages.Events.Akka;
using Signify.HBA1CPOC.Messages.Events.Status;
using Signify.HBA1CPOC.Svc.Core.Commands;
using Signify.HBA1CPOC.Svc.Core.Constants;
using Signify.HBA1CPOC.Svc.Core.Data;
using Signify.HBA1CPOC.Svc.Core.Data.Entities;
using Signify.HBA1CPOC.Svc.Core.Infrastructure.Observability;
using Signify.HBA1CPOC.Svc.Core.Models;
using Signify.HBA1CPOC.Svc.Core.Parsers;
using Signify.HBA1CPOC.Svc.Core.Queries;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Signify.HBA1CPOC.Svc.Core.EventHandlers;

/// <summary>
/// This handles Evaluation Received Event and raise HBA1CPOC Performed Event.
/// </summary>
public class HBA1CPOCEvaluationReceivedHandler : IHandleMessages<EvalReceived>
{
    private readonly ILogger _logger;
    private readonly ITransactionSupplier _transactionSupplier;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly IResultsParser _resultsParser;
    private readonly IPublishObservability _publishObservability;

    public HBA1CPOCEvaluationReceivedHandler(ILogger<HBA1CPOCEvaluationReceivedHandler> logger,
        ITransactionSupplier transactionSupplier,
        IMediator mediator,
        IMapper mapper,
        IResultsParser resultsParser,
        IPublishObservability publishObservability)
    {
        _logger = logger;
        _transactionSupplier = transactionSupplier;
        _mediator = mediator;
        _mapper = mapper;
        _resultsParser = resultsParser;
        _publishObservability = publishObservability;
    }

    [Transaction]
    public async Task Handle(EvalReceived message, IMessageHandlerContext context)
    {
        _logger.LogInformation("Start Handle EvalReceived, for EvaluationId={EvaluationId} and EventId={EventId}",
            message.EvaluationId, message.Id);

        var existing = await _mediator.Send(new GetHBA1CPOC { EvaluationId = message.EvaluationId }, context.CancellationToken);
        if (existing != null)
        {
            _logger.LogInformation("EvaluationId={EvaluationId} has already been processed, ignoring", message.EvaluationId);
            return;
        }

        var evalAnswers = await _mediator.Send(new CheckHBA1CPOCEval { EvaluationId = message.EvaluationId }, context.CancellationToken);

        message.IsLabPerformed = evalAnswers.IsHBA1CEvaluation;

        var isLabPerformed = message.IsLabPerformed ? "was" : "was not";

        _logger.LogInformation("Lab {Performed} performed, for EvaluationId={EvaluationId}", isLabPerformed, message.EvaluationId);

        var createOrUpdateCommand = _mapper.Map<CreateOrUpdateHBA1CPOC>(evalAnswers);
        createOrUpdateCommand = _mapper.Map(message, createOrUpdateCommand);

        await SetProviderInfo(message, createOrUpdateCommand);

        await SetMemberInfo(message, createOrUpdateCommand);

        var results = message.IsLabPerformed ? _resultsParser.Parse(createOrUpdateCommand.A1CPercent) : null;
        if (results != null)
            createOrUpdateCommand.NormalityIndicator = _mapper.Map<string>(results.Normality);

        using var transaction = _transactionSupplier.BeginTransaction();

        var exam = await CreateOrUpdateExam(createOrUpdateCommand, message.IsLabPerformed);

        if (message.IsLabPerformed)
        {
            await ProcessPerformed(message.Id, exam, results, context);
            PublishObservability(message, Observability.Evaluation.EvaluationPerformedEvent);
        }
        else
        {
            await ProcessNotPerformed(message.Id, exam, message);
            PublishObservability(message, Observability.Evaluation.EvaluationNotPerformedEvent);
        }

        await transaction.CommitAsync(context.CancellationToken);

        _logger.LogInformation("Finished handling EvalReceived, for EvaluationId={EvaluationId} and EventId={EventId}",
            message.EvaluationId, message.Id);

        PublishObservability(message, Observability.Evaluation.EvaluationReceivedEvent);
    }

    private async Task ProcessPerformed(Guid eventId, Data.Entities.HBA1CPOC exam, ResultsModel results, IPipelineContext context)
    {
        var performed = _mapper.Map<Performed>(exam);

        await _mediator.Send(new PublishStatusUpdate(eventId, performed), context.CancellationToken);

        await PublishResults(exam, results);

        if (exam.HBA1CPOCId > 0)
        {
            var @event = _mapper.Map<A1CPOCPerformed>(exam);
            await context.Publish(@event);
        }
    }

    private async Task ProcessNotPerformed(Guid eventId, Data.Entities.HBA1CPOC exam, EvalReceived message)
    {
        var notPerformed = _mapper.Map<NotPerformed>(exam);

        var notPerformedReasonResult =
            await _mediator.Send(new GetNotPerformedReason { EvaluationId = message.EvaluationId });
        if (notPerformedReasonResult != null)
        {
            await _mediator.Send(new AddHba1CpocNotPerformed()
            {
                NotPerformedReason = notPerformedReasonResult.NotPerformedReason,
                HBA1CPOC = exam,
                Notes = notPerformedReasonResult.ReasonNotes
            });

            notPerformed.ReasonNotes = notPerformedReasonResult.ReasonNotes;
            notPerformed.Reason = notPerformedReasonResult.Reason;
            notPerformed.ReasonType = notPerformedReasonResult.ReasonType;
        }
        else
        {
            PublishObservability(message, Observability.Evaluation.EvaluationUndefinedEvent);
        }

        await _mediator.Send(new PublishStatusUpdate(eventId, notPerformed));

        // Bill Request Not Sent
        await _mediator.Send(new CreateHBA1CPOCStatus
        {
            HBA1CPOCId = exam.HBA1CPOCId,
            StatusCodeId = HBA1CPOCStatusCode.BillRequestNotSent.HBA1CPOCStatusCodeId
        });

        var billRequestNotSent = _mapper.Map<BillRequestNotSent>(exam);
        await _mediator.Send(new PublishStatusUpdate(eventId, billRequestNotSent));
    }

    private async Task<Data.Entities.HBA1CPOC> CreateOrUpdateExam(CreateOrUpdateHBA1CPOC command, bool isPerformed)
    {
        var result = await _mediator.Send(command);

        var status = isPerformed ? HBA1CPOCStatusCode.HBA1CPOCPerformed : HBA1CPOCStatusCode.HBA1CPOCNotPerformed;

        await _mediator.Send(new CreateHBA1CPOCStatus
        {
            HBA1CPOCId = result.HBA1CPOCId,
            StatusCodeId = status.HBA1CPOCStatusCodeId
        });

        return result;
    }

    private async Task SetProviderInfo(EvalReceived message, CreateOrUpdateHBA1CPOC createOrUpdateCommand)
    {
        if (!message.ProviderId.HasValue)
            return;

        //Calling provider api for NPI
        var provider = await _mediator.Send(new GetProviderInfo { ProviderId = message.ProviderId.Value });
        createOrUpdateCommand.NationalProviderIdentifier = provider.NationalProviderIdentifier;
    }

    private async Task SetMemberInfo(EvalReceived message, CreateOrUpdateHBA1CPOC createOrUpdateCommand)
    {
        //Calling Member api for member information
        var getMemberInfo = _mapper.Map<GetMemberInfo>(message);
        var memberInfo = await _mediator.Send(getMemberInfo);
        _mapper.Map(memberInfo, createOrUpdateCommand);
    }

    private async Task PublishResults(Data.Entities.HBA1CPOC entity, ResultsModel resultsModel)
    {
        var results = _mapper.Map<ResultsReceived>(entity);

        _mapper.Map(resultsModel, results);

        await _mediator.Send(new PublishLabResults(results));
    }

    private void PublishObservability(EvalReceived message, string eventType)
    {
        var observabilityReceivedEvent = new ObservabilityEvent
        {
            EvaluationId = message.EvaluationId,
            EventType = eventType,
            EventValue = new Dictionary<string, object>
            {
                { Observability.EventParams.EvaluationId, message.EvaluationId },
                { Observability.EventParams.CreatedDateTime, message.CreatedDateTime.ToUnixTimeSeconds() }
            }
        };

        _publishObservability.RegisterEvent(observabilityReceivedEvent, true);
    }
}
