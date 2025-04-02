using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.FOBT.Svc.Core.ApiClient;
using Signify.FOBT.Svc.Core.ApiClient.Response;
using Signify.FOBT.Svc.Core.Constants;
using Signify.FOBT.Svc.Core.Data;
using Signify.FOBT.Svc.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.FOBT.Svc.Core.Queries
{
    public class GetNotPerformedReason : IRequest<NotPerformedReasonResult>
    {
        public int EvaluationId { get; set; }
    }

    /// <summary>
    /// Get Not Performed Reason.
    /// </summary>
    public class GetNotPerformedReasonHandler : IRequestHandler<GetNotPerformedReason, NotPerformedReasonResult>
    {
        private readonly IEvaluationApi _evalApi;
        private readonly FOBTDataContext _dataContext;
        private readonly ILogger<GetNotPerformedReasonHandler> _logger;
        private readonly int _maxReasonNotesLength = 1024;

        public GetNotPerformedReasonHandler(IEvaluationApi evalApi, FOBTDataContext dataContext, ILogger<GetNotPerformedReasonHandler> logger)
        {
            _evalApi = evalApi;
            _dataContext = dataContext;
            _logger = logger;
        }

        [Trace]
        public async Task<NotPerformedReasonResult> Handle(GetNotPerformedReason request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting Evaluation Answers for FOBT to get Not Performed Reason for EvaluationId={EvaluationId}",
                  request.EvaluationId);

            var evaluationVersion = await _evalApi.GetEvaluationVersion(request.EvaluationId);
            if (!(evaluationVersion?.Evaluation?.Answers.Count > 1))
            {
                return null;
            }

            var notPerformedReasonEntites = await _dataContext.NotPerformedReason.AsNoTracking().ToListAsync(cancellationToken);
            var evalAnswers = evaluationVersion.Evaluation.Answers;

            _logger.LogInformation("Matching the NotPerformed Reason with available reasons in local DB for EvaluationId={EvaluationId}",
                request.EvaluationId);

            var notPerformedReasons = from allAns in notPerformedReasonEntites
                                     join evalAns in evalAnswers on allAns.AnswerId equals evalAns.AnswerId
                                     select allAns;

            if (notPerformedReasons != null && notPerformedReasons.Any())
            {
                _logger.LogInformation("NotPerformed Reason found for EvaluationId={EvaluationId}", request.EvaluationId);

                var notPerformedReason = notPerformedReasons.FirstOrDefault();
                var reasonType = GetReasonType(notPerformedReason.AnswerId);
                var reasonNotes = GetReasonNotes(reasonType, evalAnswers);

                return new NotPerformedReasonResult(notPerformedReason, notPerformedReason.Reason, reasonNotes, reasonType);
            }

            _logger.LogInformation("NotPerformed Reason was not found for EvaluationId={EvaluationId}",
               request.EvaluationId);

            return null;
        }

        private string GetReasonType(int answerId)
        {
            var reasonType = string.Empty;
            
            switch (answerId)
            {
                case MemberRefusalType.MemberRecentlyCompleted:
                case MemberRefusalType.ScheduledToComplete:
                case MemberRefusalType.MemberApprehension:
                case MemberRefusalType.NotInterested:
                case MemberRefusalType.Other:
                    reasonType = ReasonType.MemberRefusal;
                    break;
                case MemberUnableToPerformType.TechnicalIssue:
                case MemberUnableToPerformType.EnvironmentalIssue:
                case MemberUnableToPerformType.NoSuppliesOrEquipment:
                case MemberUnableToPerformType.InsufficientTraining:
                case MemberUnableToPerformType.MemberPhysicallyUnable:
                    reasonType = ReasonType.MemberUnableToPerform;
                    break;
                default:
                    _logger.LogWarning("Unable to match AnswerId={answerId} with set Member Refusal and Unable to Perform reason types", answerId);
                    break;
            }

            return reasonType;
        }

        private string GetReasonNotes(string reasonType, IEnumerable<EvaluationAnswerModel> evaluationAnswers)
        {
            if (string.IsNullOrEmpty(reasonType) || !evaluationAnswers.Any())
            {
                return string.Empty;
            }

            var reasonNotes = string.Empty;
            switch (reasonType)
            {
                case ReasonType.MemberRefusal:
                    var memberRefusalAnswer = evaluationAnswers.FirstOrDefault(x => x.AnswerId == NotPerformedNotesAnswerId.MemberRefusalNotes);
                    if (memberRefusalAnswer != null)
                    {
                        reasonNotes = memberRefusalAnswer.AnswerValue.Length < _maxReasonNotesLength ? memberRefusalAnswer.AnswerValue : memberRefusalAnswer.AnswerValue.Substring(0, _maxReasonNotesLength);
                    }
                    break;
                case ReasonType.MemberUnableToPerform:
                    var memberUnableToPerformAnswer = evaluationAnswers.FirstOrDefault(x => x.AnswerId == NotPerformedNotesAnswerId.MemberUnableToPerformNotes);
                    if (memberUnableToPerformAnswer != null)
                    {
                        reasonNotes = memberUnableToPerformAnswer.AnswerValue.Length < _maxReasonNotesLength ? memberUnableToPerformAnswer.AnswerValue : memberUnableToPerformAnswer.AnswerValue.Substring(0, _maxReasonNotesLength);
                    }
                    break;
                default:
                    break;
            }

            return reasonNotes;
        }
    }
}