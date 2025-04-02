using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.CKD.Messages.Events;
using Signify.CKD.Svc.Core.Commands;
using Signify.CKD.Svc.Core.Constants;
using Signify.CKD.Svc.Core.Data;
using Signify.CKD.Svc.Core.Data.Entities;
using Signify.CKD.Svc.Core.Events;
using Signify.CKD.Svc.Core.Infrastructure.Observability;
using Signify.CKD.Svc.Core.Models;
using Signify.CKD.Svc.Core.Queries;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Result = Signify.CKD.Svc.Core.Messages.Result;

namespace Signify.CKD.Svc.Core.EventHandlers
{
    /// <summary>
    /// This handles Evaluation Received Event and raise CKD Performed Event.
    /// </summary>
    public class CKDEvaluationReceivedHandler : IHandleMessages<EvalReceived>
    {
        private readonly ILogger _logger;
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        private readonly IObservabilityService _observabilityService;
        private readonly ITransactionSupplier _transactionSupplier;

        public CKDEvaluationReceivedHandler(ILogger<CKDEvaluationReceivedHandler> logger,
            IMediator mediator,
            IMapper mapper,
            IObservabilityService observabilityService,
            ITransactionSupplier transactionSupplier)
        {
            _logger = logger;
            _mediator = mediator;
            _mapper = mapper;
            _observabilityService = observabilityService;
            _transactionSupplier = transactionSupplier;
        }

        [Transaction]
        public async Task Handle(EvalReceived message, IMessageHandlerContext context)
        {
            _logger.LogDebug("Start Handle EvalReceived, for EvaluationId={EvaluationId}", message.EvaluationId);

            // Query CKD by evaluation id
            var existingCkd = await _mediator.Send(new GetCKD { EvaluationId = message.EvaluationId });
            if (existingCkd != null)
            {
                await UpdateDateOfService(message.DateOfService, existingCkd, message.Id, context);

                _logger.LogDebug("End Handle EvalReceived, for EvaluationId={EvaluationId}", message.EvaluationId);

                _observabilityService.AddEvent(Observability.Evaluation.EvaluationClarificationEvent, new Dictionary<string, object>()
                {
                    {Observability.EventParams.EvaluationId, message.EvaluationId},
                    {Observability.EventParams.CreatedDateTime, message.CreatedDateTime.ToUnixTimeSeconds()}
                });

                return;
            }

            var evalAnswers = await _mediator.Send(new CheckCKDEval { EvaluationId = message.EvaluationId });

            _logger.LogInformation("CKD lab {IsPerformed} performed, for EvaluationId={EvaluationId}, EventId={EventId}",
                evalAnswers.IsCKDEvaluation ? "was" : "was not", message.EvaluationId, message.Id);

            var createOrUpdate = _mapper.Map<CreateOrUpdateCKD>(evalAnswers);
            createOrUpdate = _mapper.Map(message, createOrUpdate);

            await SetProviderInfo(message, createOrUpdate);

            await SetMemberInfo(message, createOrUpdate);

            using var transaction = _transactionSupplier.BeginTransaction();

            // Create CKD row and log CKD status "CKD Performed" or "CKD NotPerformed"
            var entity = await CreateEntity(evalAnswers, createOrUpdate);

            await PublishStatusEvent(message, evalAnswers, entity, context);

            if (evalAnswers.IsCKDEvaluation)
            {
                await PublishResultEvent(evalAnswers, entity);
            }

            _logger.LogInformation("End Handle EvalReceived, for EvaluationId={EvaluationId}", message.EvaluationId);

            await transaction.CommitAsync();

            _observabilityService.AddEvent(Observability.Evaluation.EvaluationReceivedEvent, new Dictionary<string, object>()
            {
                {Observability.EventParams.EvaluationId, message.EvaluationId},
                {Observability.EventParams.CreatedDateTime, message.CreatedDateTime.ToUnixTimeSeconds()}
            });
        }

        private async Task UpdateDateOfService(DateTime? eventDos, Data.Entities.CKD ckd, Guid eventId, IPipelineContext context)
        {
            if (Nullable.Compare(eventDos, ckd.DateOfService) == 0)
            {
                _logger.LogDebug("Nothing to do, DateOfService was not changed, for EvaluationId={EvaluationId}, EventId={EventId}",
                    ckd.EvaluationId, eventId);
                return;
            }

            if (eventDos == null)
            {
                _logger.LogInformation("Evaluation exists, but DateOfService is null so nothing to do, for EvaluationId={EvaluationId}, EventId={EventId}",
                    ckd.EvaluationId, eventId);
                return;
            }

            await context.SendLocal(new DateOfServiceUpdated(ckd.EvaluationId!.Value, eventDos.Value));
            _logger.LogInformation("Evaluation exists and DateOfService update event published, for EvaluationId={EvaluationId}, EventId={EventId}",
                ckd.EvaluationId, eventId);
        }

        /// <summary>
        /// Calls Provider API to append NPI
        /// </summary>
        /// <param name="message"></param>
        /// <param name="createOrUpdate"></param>
        private async Task SetProviderInfo(EvalReceived message, CreateOrUpdateCKD createOrUpdate)
        {
            if (!message.ProviderId.HasValue)
                return;

            var provider = await _mediator.Send(new GetProviderInfo { ProviderId = message.ProviderId.Value });
            createOrUpdate.NationalProviderIdentifier = provider.NationalProviderIdentifier;
        }

        /// <summary>
        /// Calls Member API to append member details
        /// </summary>
        /// <param name="message"></param>
        /// <param name="createOrUpdate"></param>
        private async Task SetMemberInfo(EvalReceived message, CreateOrUpdateCKD createOrUpdate)
        {
            var getMemberInfo = _mapper.Map<GetMemberInfo>(message);
            var memberInfo = await _mediator.Send(getMemberInfo);
            _mapper.Map(memberInfo, createOrUpdate);
        }

        private async Task<Data.Entities.CKD> CreateEntity(EvaluationAnswers answers, CreateOrUpdateCKD createOrUpdate)
        {
            var entity = await _mediator.Send(createOrUpdate);

            var statusCode = answers.IsCKDEvaluation ? CKDStatusCode.CKDPerformed : CKDStatusCode.CKDNotPerformed;

            await _mediator.Send(new CreateCKDStatus
            {
                CKDId = entity.CKDId,
                StatusCodeId = statusCode.CKDStatusCodeId
            });

            return entity;
        }

        private Task PublishStatusEvent(EvalReceived message, EvaluationAnswers answers, Data.Entities.CKD entity, IPipelineContext context)
        {
            if (answers.IsCKDEvaluation)
            {
                var performed = _mapper.Map<CKDPerformed>(entity);
                return context.SendLocal(performed);
            }

            return context.SendLocal(new ExamNotPerformedEvent
            {
                Exam = entity,
                EventId = message.Id,
                NotPerformedReasonId = answers.NotPerformedReasonId!.Value,
                NotPerformedReasonNotes = answers.NotPerformedNotes
            });
        }

        private Task PublishResultEvent(EvaluationAnswers answers, Data.Entities.CKD entity)
        {
            var result = _mapper.Map<Result>(entity);
            _mapper.Map(answers.LookupCKDAnswerEntity, result);
            return _mediator.Send(new PublishResult(result));
        }
    }
}
