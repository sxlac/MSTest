using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Signify.PAD.Messages.Events;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Constants;
using Signify.PAD.Svc.Core.Data;
using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.Events;
using Signify.PAD.Svc.Core.Models;
using Signify.PAD.Svc.Core.Queries;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NotPerformed = Signify.PAD.Svc.Core.Data.Entities.NotPerformed;

namespace Signify.PAD.Svc.Core.EventHandlers;

/// <summary>
/// This handles Evaluation Received Event and raise PAD Performed Event.
/// </summary>
public class PadEvaluationReceivedHandler : IHandleMessages<EvalReceived>
{
    private readonly ILogger _logger;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly PADDataContext _dataContext;
    private readonly ITransactionSupplier _transactionSupplier;
    private readonly IPublishObservability _publishObservability;

    public PadEvaluationReceivedHandler(
        ILogger<PadEvaluationReceivedHandler> logger,
        IMediator mediator,
        PADDataContext dataContext,
        IMapper mapper,
        ITransactionSupplier transactionSupplier,
        IPublishObservability publishObservability)
    {
        _logger = logger;
        _mediator = mediator;
        _mapper = mapper;
        _dataContext = dataContext;
        _transactionSupplier = transactionSupplier;
        _publishObservability = publishObservability;
    }

    [Transaction]
    public async Task Handle(EvalReceived evalReceived, IMessageHandlerContext context)
    {
        _logger.LogDebug("Start Handle PADEvaluationReceived for EvaluationId={EvaluationId}", evalReceived.EvaluationId);

        //Query PAD by evaluation id
        var existing = await _dataContext.PAD.FirstOrDefaultAsync(s => s.EvaluationId == evalReceived.EvaluationId, context.CancellationToken);

        if (existing != null)
        {
            _logger.LogDebug("Evaluation already processed, EvaluationId={EvaluationId}", evalReceived.EvaluationId);
        }
        else
        {
            var evalAnswers = await _mediator.Send(new QueryEvaluationAnswers { EvaluationId = evalReceived.EvaluationId }, context.CancellationToken);
            var createOrUpdate = _mapper.Map<CreateOrUpdatePAD>(evalAnswers);
            createOrUpdate = _mapper.Map(evalReceived, createOrUpdate);
            Data.Entities.PAD pad;

            if (evalReceived.ProviderId.HasValue)
            {
                //Calling provider api for NPI
                var provider = await _mediator.Send(new GetProviderInfo
                {
                    ProviderId = evalReceived.ProviderId.Value
                }, context.CancellationToken);

                createOrUpdate.NationalProviderIdentifier = provider.NationalProviderIdentifier;
            }

            //Calling Member api for member information
            var getMemberInfo = _mapper.Map<GetMemberInfo>(evalReceived);
            var memberInfo = await _mediator.Send(getMemberInfo, context.CancellationToken);
            createOrUpdate = _mapper.Map(memberInfo, createOrUpdate);

            //Update the status LabNotPerformed if IsPADEvaluation is false
            var status = evalAnswers.IsPadPerformedToday ? PADStatusCode.PadPerformed : PADStatusCode.PadNotPerformed;

            using var transaction = _transactionSupplier.BeginTransaction();

            // Create PAD
            pad = await _mediator.Send(createOrUpdate, context.CancellationToken);

            // Create AoeSymptomSupportResult
            var aoeSymptomSupportResult = _mapper.Map<AoeSymptomSupportResult>(evalAnswers.AoeSymptomAnswers);
            if (aoeSymptomSupportResult != null) 
            {
                aoeSymptomSupportResult.PADId = pad.PADId;
                await _mediator.Send(new CreateAoeSymptomSupportResult { AoeSymptomSupportResult = aoeSymptomSupportResult }, context.CancellationToken);

                var aoeResult = _mapper.Map<AoeResult>(evalReceived);
                _mapper.Map(evalAnswers.AoeSymptomAnswers, aoeResult);
                await _mediator.Send(new PublishAoeResult(aoeResult), context.CancellationToken);
            }

            // Create PAD status
            await _mediator.Send(new CreatePadStatus
            {
                PadId = pad.PADId,
                StatusCode = status
            }, context.CancellationToken);

            if (evalAnswers.NotPerformedAnswerId.HasValue)
            {
                await _mediator.Send(new CreateNotPerformed
                {
                    NotPerformedRec = new NotPerformed
                    {
                        PADId = pad.PADId,
                        AnswerId = evalAnswers.NotPerformedAnswerId.Value,
                        Notes = evalAnswers.NotPerformedNotes
                    }
                }, context.CancellationToken);
            }

            //Publish PAD status code
            await PublishPadStatus(evalAnswers, pad, status, context.CancellationToken);

            //Publish PAD result
            if (status.StatusCode == Application.PADPerformed)
                await PublishResults(evalAnswers, pad, context.CancellationToken);

            if (pad.PADId < 1)
            {
                _logger.LogError("Something went wrong while saving the PADEvaluationReceived, EvaluationId={EvaluationId}", evalReceived.EvaluationId);
            }
            await transaction.CommitAsync(context.CancellationToken);
        }
        _logger.LogDebug("End Handle PADEvaluationReceived for EvaluationId={EvaluationId}", evalReceived.EvaluationId);

        PublishObservability(evalReceived, Observability.Evaluation.EvaluationReceivedEvent, sendImmediate: true);
    }

    private async Task PublishPadStatus(Models.EvaluationAnswers evalAnswers, Data.Entities.PAD pad, PADStatusCode status, CancellationToken cancellationToken)
    {
        switch (status.StatusCode)
        {
            case Application.PADPerformed:
                var publishPadPerformed = _mapper.Map<Performed>(pad);
                await _mediator.Send(publishPadPerformed, cancellationToken);
                break;
            case Application.PADNotPerformed:
                var publishPadNotPerformed = _mapper.Map<Core.Commands.NotPerformed>(pad);
                publishPadNotPerformed.ReasonNotes = evalAnswers.NotPerformedNotes;
                publishPadNotPerformed.ReasonType = evalAnswers.NotPerformedReasonType;
                publishPadNotPerformed.Reason = evalAnswers.NotPerformedReason;
                await _mediator.Send(publishPadNotPerformed, cancellationToken);
                break;
        }
    }

    private async Task PublishResults(Models.EvaluationAnswers evalAnswers, Data.Entities.PAD pad, CancellationToken cancellationToken)
    {
        var resultEvent = _mapper.Map<ResultsReceived>(pad);
        _mapper.Map(evalAnswers, resultEvent);

        await _mediator.Send(new PublishResults(resultEvent), cancellationToken);
    }

    private void PublishObservability(EvalReceived message, string eventType, bool sendImmediate = false)
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

        _publishObservability.RegisterEvent(observabilityReceivedEvent, sendImmediate);
    }
}