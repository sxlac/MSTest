using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Constants.Questions;
using Signify.Spirometry.Core.Exceptions;
using Signify.Spirometry.Core.Models;
using Signify.Spirometry.Core.Queries;
using SpiroNsbEvents;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Signify.Spirometry.Core.Constants;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers
{
    /// <summary>
    /// Event handler that coordinates what to do when this process manager receives an evaluation over Kafka
    /// that contains a Spirometry product.
    /// </summary>
    public class EvalReceivedHandler : IHandleMessages<EvalReceived>
    {
        private readonly ILogger _logger;
        private readonly IMediator _mediator;
        private readonly IPublishObservability _publishObservability;
        
        public EvalReceivedHandler(ILogger<EvalReceivedHandler> logger,
            IMediator mediator,
            IPublishObservability publishObservability)
        {
            _logger = logger;
            _mediator = mediator;
            _publishObservability = publishObservability;
        }

        [Transaction]
        public async Task Handle(EvalReceived message, IMessageHandlerContext context)
        {
            _logger.LogDebug("Start Handle for EvaluationId={EvaluationId}", message.EvaluationId);
            try
            {
                if (await ExamExistsInDatabase(message, context.CancellationToken))
                {
                    await HandleUpdatedEvaluation(message, context.CancellationToken);
                }
                else
                {
                    await HandleNewEvaluation(message, context);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Message} - For EvaluationId={EvaluationId}", ex.Message, message.EvaluationId);
                throw;
            }
            finally
            {
                _logger.LogDebug("End Handle for EvaluationId={EvaluationId}", message.EvaluationId);
            }
        }

        private async Task<bool> ExamExistsInDatabase(EvalReceived message, CancellationToken token)
        {
            return await _mediator.Send(new QuerySpirometryExam(message.EvaluationId), token) != null;
        }

        private async Task HandleUpdatedEvaluation(EvalReceived message, CancellationToken token)
        {
            await _mediator.Send(new UpdateExam(message), token);
            
            PublishObservability(message, Observability.Evaluation.EvaluationClarificationEvent);
        }

        private async Task HandleNewEvaluation(EvalReceived message, IPipelineContext context)
        {
            // Query Evaluation API for the evaluation answers, and create the ExamModel
            ExamModel examModel;
            try
            {
                examModel = await _mediator.Send(new GenerateExamModel(message.EvaluationId), context.CancellationToken);
            }
            catch (RequiredEvaluationQuestionMissingException ex)
            {
                if (ex.QuestionId != SpirometryTestPerformedQuestion.QuestionId)
                    throw; // All other required Q's

                // With the exception of an edge case (ex issue in the form), this happens for VHRA (virtual vs in-home)
                // evaluations. These appointments shouldn't be scheduled with Spirometry because that can only be tested
                // in-person and not virtually, but until those start going away (we have significantly fewer coming in now),
                // this is here so we don't get all those going to the error queue. I'll consider tweaking the product
                // code filter to also ignore evaluations if they contain both VHRA and Spiro, but not doing that quite yet.
                _logger.LogWarning("EvaluationId={EvaluationId} answers do not include expected QuestionId={QuestionId} (\"Spirometry Performed?\") for an appointment that was to include Spirometry; ignoring",
                    message.EvaluationId, ex.QuestionId);
                
                PublishObservability(message, Observability.Evaluation.EvaluationUndefinedEvent);
                return;
            }

            // Aggregate info from EvalReceived, Provider info, and Member info
            var spirometryExam = await _mediator.Send(new AggregateSpirometryExamDetails(message), context.CancellationToken);
            spirometryExam.FormVersionId = examModel.FormVersionId;

            var wasPerformed = examModel is PerformedExamModel;

            if (wasPerformed)
            {
                await context.SendLocal(new ExamPerformedEvent
                {
                    EventId = message.Id,
                    Exam = spirometryExam,
                    Result = ((PerformedExamModel)examModel).ExamResult
                });
            }
            else
            {
                await context.SendLocal(new ExamNotPerformedEvent
                {
                    EventId = message.Id,
                    Exam = spirometryExam,
                    Info = ((NotPerformedExamModel)examModel).NotPerformedInfo
                });
            }

            _logger.LogInformation("Event queued for processing an evaluation where an exam {Performed} performed, for EvaluationId={EvaluationId}",
                wasPerformed ? "was" : "was not",
                spirometryExam.EvaluationId);
            
            PublishObservability(message, Observability.Evaluation.EvaluationReceivedEvent);
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
}
