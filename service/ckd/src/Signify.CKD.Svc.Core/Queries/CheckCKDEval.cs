using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.CKD.Svc.Core.ApiClient;
using Signify.CKD.Svc.Core.Constants;
using Signify.CKD.Svc.Core.Data;
using Signify.CKD.Svc.Core.Infrastructure.Observability;
using Signify.CKD.Svc.Core.Models;
using Signify.CKD.Svc.Core.Queries.Helpers;

namespace Signify.CKD.Svc.Core.Queries
{
    public class CheckCKDEval : IRequest<EvaluationAnswers>
    {
        public int EvaluationId { get; set; }
    }

    /// <summary>
    /// Check if CKD delivered and barcode exists from Evaluation answers.
    /// </summary>
    public class CheckCKDEvalHandler : IRequestHandler<CheckCKDEval, EvaluationAnswers>
    {
        private readonly ILogger _logger;
        private readonly IEvaluationApi _evalApi;
        private readonly CKDDataContext _dataContext;
        private readonly IObservabilityService _observabilityService;
        
        //private List<int> AnswerIds = new List<int> { 20962, 20963, 20964, 20965, 20966, 20967, 20968, 20969, 20970, 20971, 20972, 20973, 20974, 20975, 20976, 20977, 20978, 20979, 20980, 20981 };

        public CheckCKDEvalHandler(ILogger<CheckCKDEvalHandler> logger, IEvaluationApi evalApi, CKDDataContext dataContext, IObservabilityService observabilityService)
        {
            _logger = logger;
            _evalApi = evalApi;
            _dataContext = dataContext;
            _observabilityService = observabilityService;
        }

        [Trace]
        public async Task<EvaluationAnswers> Handle(CheckCKDEval request, CancellationToken cancellationToken)
        {
            using var _ = _logger.BeginScope("EvaluationId={EvaluationId}", request.EvaluationId);

            // See https://wiki.signifyhealth.com/display/AncillarySvcs/CKD+Form+Questions

            var evaluation = new EvaluationAnswers();
            var evalVerRs = await _evalApi.GetEvaluationVersion(request.EvaluationId);

            if (evalVerRs?.Evaluation?.Answers == null || evalVerRs.Evaluation.Answers.Count < 1)
            {
                _logger.LogWarning("Evaluation API did not return any answers for this evaluation");
                return evaluation;
            }

            var lookup = new Lookup(evalVerRs.Evaluation.Answers);

            _logger.LogInformation("{Count} evaluation questions were answered", lookup.AnswersByQuestionId.Count);

            bool isUrineCollectedToday = lookup.HasQuestion(463,
                answer => answer.AnswerValue == "1" && answer.AnswerId == 20950);

            if (!isUrineCollectedToday)
            {
                await SetNotPerformedReasonAndNotes(request.EvaluationId, evaluation, lookup, cancellationToken);
                return evaluation;
            }

            // Get result for urine micro albumin dipstick value
            if (!lookup.HasQuestion(468, out var evalAnswers))
            {
                _logger.LogWarning("Lab was performed, but results question was not answered");
            }
            else
            {
                //Comparing the Answer id with local look Up Id for match and select the respective lookup answer Entity
                evaluation.LookupCKDAnswerEntity = _dataContext.LookupCKDAnswer.FirstOrDefault(a => a.CKDAnswerId == evalAnswers.First().AnswerId);
                if (evaluation.LookupCKDAnswerEntity == null)
                {
                    _logger.LogWarning("Lab was performed, but results AnswerId={AnswerId} was not found in lookup table", evalAnswers.First().AnswerId);
                }
            }

            evaluation.IsCKDEvaluation = isUrineCollectedToday;

            if (TryGetExpiryDate(lookup, out var expiry))
            {
                evaluation.ExpirationDate = expiry;
            }

            return evaluation;
        }

        private static bool TryGetExpiryDate(Lookup lookup, out DateTime expiry)
        {
            if (lookup.HasQuestion(91550, answer => answer.AnswerId == 33263 && !string.IsNullOrEmpty(answer.AnswerValue), out var evalAnswers))
            {
                var cultureInfo = new CultureInfo("en-US");
                if (DateTime.TryParse(evalAnswers.First().AnswerValue, cultureInfo, DateTimeStyles.None, out expiry))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            expiry = default;
            return false;
        }


        private async Task SetNotPerformedReasonAndNotes(int evaluationId, EvaluationAnswers result, Lookup lookup, CancellationToken token)
        {
            // Not Performed Notes
            const int notesMemberRefusedQuestionId = 90655;
            const int notesMemberUnableToPerformQuestionId = 90658;

            //Answers related to 'Member physically unable'
            const int physicallyUnableAnswerId = 50899;
            const int physicallyUnableAnswerIdForNotes = 50900;

            if (lookup.HasQuestion(notesMemberRefusedQuestionId, out var notes) ||
                lookup.HasQuestion(notesMemberUnableToPerformQuestionId, out notes))
            {
                result.NotPerformedNotes = notes.First().AnswerValue;
            }

            const int reasonMemberRefusedQuestionId = 90654;
            const int reasonUnableToPerformQuestionId = 90657;
            if (!lookup.HasQuestion(reasonMemberRefusedQuestionId, out var answers) &&
                !lookup.HasQuestion(reasonUnableToPerformQuestionId, out answers))
            {
                _logger.LogWarning("EvaluationId {EvaluationId} was Not Performed, but no not performed reason AnswerId could be determined, this will error out", evaluationId);
                
                _observabilityService.AddEvent(Observability.Evaluation.EvaluationUndefinedEvent, new Dictionary<string, object>()
                {
                    {Observability.EventParams.EvaluationId, evaluationId},
                    {Observability.EventParams.CreatedDateTime, DateTimeOffset.Now.ToUnixTimeSeconds()}
                });
            }

            result.NotPerformedAnswerId = answers.First().AnswerId;

            if (result.NotPerformedAnswerId == physicallyUnableAnswerId) //If in home clinician selected this answer, then Notes should come from a exclusive dedicated AnswerId 
            {
                //QuestionId is the same as 'Member Unable to Perform'
                _logger.LogInformation("EvaluationId {EvaluationId} was Not Performed since member was physically unable. This answer has additional exclusive notes", evaluationId);
                var haveNotesForPhysicallyUnable = lookup.HasQuestion(reasonUnableToPerformQuestionId, answer => answer.AnswerId == physicallyUnableAnswerIdForNotes && !string.IsNullOrEmpty(answer.AnswerValue), out var notesforPhysicallyUnable);

                if (haveNotesForPhysicallyUnable)
                {
                    result.NotPerformedNotes = notesforPhysicallyUnable.First().AnswerValue;
                }
            }

            var reason = await _dataContext.NotPerformedReason
            .AsNoTracking()
            .FirstAsync(each => each.AnswerId == answers.First().AnswerId, token);

            result.NotPerformedReasonId = reason.NotPerformedReasonId;
        }
    }
}
