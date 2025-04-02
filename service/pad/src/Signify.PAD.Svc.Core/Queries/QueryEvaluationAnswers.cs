using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Signify.PAD.Svc.Core.ApiClient;
using Signify.PAD.Svc.Core.ApiClient.Response;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Constants;
using Signify.PAD.Svc.Core.Constants.Questions;
using Signify.PAD.Svc.Core.Exceptions;
using Signify.PAD.Svc.Core.Models;
using Signify.PAD.Svc.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.PAD.Svc.Core.Queries;

public class QueryEvaluationAnswers : IRequest<EvaluationAnswers>
{
    public int EvaluationId { get; set; }
}

public class QueryEvaluationAnswersHandler : IRequestHandler<QueryEvaluationAnswers, EvaluationAnswers>
{
    private readonly ILogger _logger;
    private readonly IEvaluationApi _evaluationApi;
    private readonly IBuildAnswerLookup _lookupBuilder;
    private readonly IMediator _mediator;
    private readonly IPublishObservability _publishObservability;

    public QueryEvaluationAnswersHandler(ILogger<QueryEvaluationAnswersHandler> logger,
        IEvaluationApi evaluationApi,
        IBuildAnswerLookup lookupBuilder,
        IMediator mediator,
        IPublishObservability publishObservability)
    {
        _logger = logger;
        _evaluationApi = evaluationApi;
        _lookupBuilder = lookupBuilder;
        _mediator = mediator;
        _publishObservability = publishObservability;
    }

    [Trace]
    public async Task<EvaluationAnswers> Handle(QueryEvaluationAnswers request, CancellationToken cancellationToken)
    {
        var evaluationVersion = await _evaluationApi.GetEvaluationVersion(request.EvaluationId);
        if (evaluationVersion?.Evaluation?.Answers == null || evaluationVersion.Evaluation.Answers.Count < 1)
        {
            // Let this finalized evaluation go to the error queue for retry; all evaluations must have at least _some_ answers
            throw new NoEvaluationAnswersExistException(request.EvaluationId);
        }

        _logger.LogInformation("Received evaluation version from the Evaluation API for EvaluationId={EvaluationId} with {AnswerCount} answers",
            request.EvaluationId, evaluationVersion.Evaluation.Answers.Count);

        var allAnswers = _lookupBuilder.BuildLookup(evaluationVersion.Evaluation.Answers);

        // Pad has a question that can prevent the performed/not performed question from showing up. Check for that first.
        if (TryGetOptional(allAnswers, PadDiagnosisConfirmedClinicallyQuestion.QuestionId, out var padDiagnosicFromClinical))
        {
            var padTestNotNeededResult = new EvaluationAnswers()
            {
                IsPadPerformedToday = false,
                NotPerformedAnswerId = padDiagnosicFromClinical.AnswerId,
                NotPerformedReason = PadDiagnosisConfirmedClinicallyQuestion.Reason,
                NotPerformedReasonType = PadDiagnosisConfirmedClinicallyQuestion.Reason,
                NotPerformedNotes = string.Empty,
                AoeSymptomAnswers = await _mediator.Send(new ParseAoeSymptomResults(request.EvaluationId, allAnswers, new EvaluationAnswers()), cancellationToken)
            };
            return padTestNotNeededResult;
        }

        var isPadPerformedToday = WasPadPerformedToday(request, allAnswers);

        var result = isPadPerformedToday
            ? await _mediator.Send(new ParsePerformedResults(request.EvaluationId, allAnswers), cancellationToken)
            : await _mediator.Send(new ParseNotPerformedResults(request.EvaluationId, allAnswers), cancellationToken);

        result.AoeSymptomAnswers = await _mediator.Send(new ParseAoeSymptomResults(request.EvaluationId, allAnswers, result), cancellationToken);

        return result;
    }

    private bool WasPadPerformedToday(QueryEvaluationAnswers request, IReadOnlyDictionary<int, ICollection<EvaluationAnswerModel>> allAnswers)
    {
        const int qId = PadTestPerformedQuestion.QuestionId; // "Peripheral arterial disease testing performed today?" question id

        if (!TryGetOptional(allAnswers, qId, out var wasPerformedQ))
        {
            _logger.LogWarning("Evaluation with a PAD product does not have the \"{QuestionText}\" question ({QuestionId}) answered, for EvaluationId={EvaluationId}",
                PadTestPerformedQuestion.QuestionText, qId, request.EvaluationId);

            // Not changing the implementation today; I'll soon change this to throw
            // an exception if a different AnswerId is given, or if this question was
            // not answered at all (granted, VHRA issues, but that should be going away).
            return false;
        }

        switch (wasPerformedQ.AnswerId)
        {
            case PadTestPerformedQuestion.YesAnswerId:
                return true;
            case PadTestPerformedQuestion.NoAnswerId:
                return false;
            default:
                _logger.LogWarning("Evaluation with a PAD product has an unknown answer to the \"{QuestionText}\" question ({QuestionId}) - AnswerId={AnswerId}, with AnswerValue={AnswerValue}, for EvaluationId={EvaluationId}",
                    PadTestPerformedQuestion.QuestionText, qId, wasPerformedQ.AnswerId, wasPerformedQ.AnswerValue, request.EvaluationId);

                PublishObservability(request.EvaluationId, DateTimeOffset.Now.ToUnixTimeSeconds(), Observability.Evaluation.EvaluationUndefinedEvent, sendImmediate: true);

                throw new UnsupportedAnswerForQuestionException(request.EvaluationId, wasPerformedQ.QuestionId, wasPerformedQ.AnswerId, wasPerformedQ.AnswerValue);
        }
    }

    private static bool TryGetOptional(IReadOnlyDictionary<int, ICollection<EvaluationAnswerModel>> allAnswers, int questionId, out EvaluationAnswerModel answerModel)
    {
        if (allAnswers.TryGetValue(questionId, out var answers))
        {
            answerModel = answers.First();
            return true;
        }

        answerModel = default;
        return false;
    }

    private void PublishObservability(int evaluationId, long createdDateTime, string eventType, bool sendImmediate = false)
    {
        var observabilityDosUpdatedEvent = new ObservabilityEvent
        {
            EvaluationId = evaluationId,
            EventType = eventType,
            EventValue = new Dictionary<string, object>
            {
                {Observability.EventParams.EvaluationId, evaluationId},
                {Observability.EventParams.CreatedDateTime, createdDateTime}
            }
        };

        _publishObservability.RegisterEvent(observabilityDosUpdatedEvent, sendImmediate);
    }
}