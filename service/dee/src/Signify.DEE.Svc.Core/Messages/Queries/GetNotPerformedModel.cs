using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.DEE.Svc.Core.ApiClient.Responses;
using Signify.DEE.Svc.Core.Constants;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Messages.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.Messages.Queries;

public class GetNotPerformedModel : IRequest<NotPerformedModel>
{
    public long EvaluationId { get; set; }
    public List<EvaluationAnswer> Answers { get; set; }
}

/// <summary>
/// Get Not Performed Model
/// </summary>
public class GetNotPerformedModelHandler(
    DataContext context,
    ILogger<GetNotPerformedModelHandler> logger,
    IMapper mapper)
    : IRequestHandler<GetNotPerformedModel, NotPerformedModel>
{
    [Trace]
    public async Task<NotPerformedModel> Handle(GetNotPerformedModel request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting Evaluation Answers for DEE to get Not Performed Model for EvaluationId={EvaluationId}",
            request.EvaluationId);

        if (!request.Answers.Any()) return null;

        var notPerformedReasons = await context.NotPerformedReason.AsNoTracking().ToListAsync(cancellationToken);

        var evalAnswers = request.Answers;

        logger.LogInformation("Matching the NotPerformed Reason with available reasons in local DB for EvaluationId={EvaluationId}",
            request.EvaluationId);

        NotPerformedReason reason;
        var notPerformedOtherAnswer = evalAnswers.Find(a => a.AnswerId == ReasonMemberRefusedQuestion.Other || a.AnswerId == ReasonUnableToPerformQuestion.Other);
        if (notPerformedOtherAnswer is not null)
        {
            reason = notPerformedReasons.First(npr => npr.AnswerId == notPerformedOtherAnswer.AnswerId);
        }
        else
        {
            reason = (from allAns in notPerformedReasons
                      join evalAns in evalAnswers on allAns.AnswerId equals evalAns.AnswerId
                      select allAns).FirstOrDefault();
        }

        logger.LogInformation("Not performed reason {WasFound} found for EvaluationId={EvaluationId}",
            reason != null ? "was" : "was not", request.EvaluationId);

        if (reason == null) return null;

        var notPerformedModel = mapper.Map<NotPerformedModel>(reason);
        var reasonType = GetReasonType(reason.AnswerId);
        notPerformedModel.ReasonType = reasonType;
        notPerformedModel.ReasonNotes = GetReasonNotes(reasonType, evalAnswers);

        return notPerformedModel;
    }

    private string GetReasonType(int answerId)
    {
        var reasonType = string.Empty;

        switch (answerId)
        {
            case ReasonMemberRefusedQuestion.MemberRecentlyCompleted:
            case ReasonMemberRefusedQuestion.ScheduledToComplete:
            case ReasonMemberRefusedQuestion.MemberApprehension:
            case ReasonMemberRefusedQuestion.NotInterested:
            case ReasonMemberRefusedQuestion.Other:
                reasonType = ReasonType.MemberRefusal;
                break;
            case ReasonUnableToPerformQuestion.TechnicalIssue:
            case ReasonUnableToPerformQuestion.EnvironmentalIssue:
            case ReasonUnableToPerformQuestion.NoSuppliesOrEquipment:
            case ReasonUnableToPerformQuestion.InsufficientTraining:
            case ReasonUnableToPerformQuestion.MemberPhysicallyUnable:
            case ReasonUnableToPerformQuestion.Other:
                reasonType = ReasonType.MemberUnableToPerform;
                break;
            default:
                logger.LogWarning("Unable to match AnswerId={AnswerId} with set Member Refusal and Unable to Perform reason types", answerId);
                break;
        }

        return reasonType;
    }

    private static string GetReasonNotes(string reasonType, ICollection<EvaluationAnswer> evaluationAnswers)
    {
        if (string.IsNullOrEmpty(reasonType) || evaluationAnswers.Count == 0)
        {
            return string.Empty;
        }

        // When the provider chooses "other" as the reason for not performed (within the categories of member refused or unable to perform)
        // it is because one of the predefined values don't fit so we ask them to populate a notes field for clarification.
        // We'll fetch those as the reason notes when "Other" is the reason an exam is not performed.
        // Otherwise, if they choose a predefined not performed reason, we'll fetch a different set of notes that provide additional context
        // i.e. "Insufficient Training" might have notes like "My training certificate expired."

        switch (reasonType)
        {
            case ReasonType.MemberRefusal:
                var priorityNotes = evaluationAnswers.FirstOrDefault(a => a.AnswerId == ReasonMemberRefusedQuestion.OtherNotes);
                if (priorityNotes is not null && !string.IsNullOrEmpty(priorityNotes.AnswerValue))
                {
                    return priorityNotes.AnswerValue;
                }
                var memberRefusalAnswer = evaluationAnswers.FirstOrDefault(x => x.AnswerId == NotPerformedNotesAnswerId.MemberRefusalNotes);
                if (memberRefusalAnswer is not null && !string.IsNullOrEmpty(memberRefusalAnswer.AnswerValue))
                {
                    return memberRefusalAnswer.AnswerValue;
                }
                break;
            case ReasonType.MemberUnableToPerform:
                var priorityUnableToPerformNotes = evaluationAnswers.FirstOrDefault(a => a.AnswerId == ReasonUnableToPerformQuestion.OtherNotes);
                if (priorityUnableToPerformNotes is not null && !string.IsNullOrEmpty(priorityUnableToPerformNotes.AnswerValue))
                {
                    return priorityUnableToPerformNotes.AnswerValue;
                }
                var memberUnableToPerformAnswer = evaluationAnswers.FirstOrDefault(x => x.AnswerId == NotPerformedNotesAnswerId.MemberUnableToPerformNotes);
                if (memberUnableToPerformAnswer != null && !string.IsNullOrEmpty(memberUnableToPerformAnswer.AnswerValue))
                {
                    return memberUnableToPerformAnswer.AnswerValue;
                }
                break;
        }

        return string.Empty;
    }
}