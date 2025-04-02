using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.DEE.Svc.Core.ApiClient;
using Signify.DEE.Svc.Core.Messages.Models;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.Messages.Queries;

[ExcludeFromCodeCoverage]
public class GetEvalAnswers : IRequest<ExamAnswersModel>
{
    public long EvaluationId { get; set; }
}

public class GetEvalAnswersHandler(
    ILogger<GetEvalAnswersHandler> logger,
    IEvaluationApi evaluationApi,
    IProviderApi providerApi)
    : IRequestHandler<GetEvalAnswers, ExamAnswersModel>
{
    [Trace]
    public async Task<ExamAnswersModel> Handle(GetEvalAnswers request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Sending request to Evaluation API to get latest version for EvaluationId {EvaluationId}", request.EvaluationId);
        var evaVersion = await evaluationApi.GetEvaluationVersion(request.EvaluationId);

        if (!evaVersion.IsSuccessStatusCode || evaVersion.Content == null)
        {
            logger.LogWarning("No Evaluation Version returned from Evaluation API for EvaluationId {EvaluationId}", request.EvaluationId);
            return null;
        }

        var evaluation = evaVersion.Content.Evaluation;

        logger.LogInformation("Evaluation API returned latest version for EvaluationId {EvaluationId}, with {AnswerCount} answers",
            request.EvaluationId, evaluation.Answers.Count);

        logger.LogDebug("Sending request to Provider API to get provider info for EvaluationId {EvaluationId}, ProviderId {ProviderId}",
            request.EvaluationId, evaluation.ProviderId);

        var provider = await providerApi.GetProviderById(evaluation.ProviderId);

        if (!provider.IsSuccessStatusCode || provider.Content == null)
        {
            logger.LogWarning("No provider info returned from Provider API for EvaluationId {EvaluationId}, ProviderId {ProviderId}",
                request.EvaluationId, evaluation.ProviderId);
            return null;
        }


        var answers = new ExamAnswersModel
        {
            DateOfService = evaluation.DateOfService.GetValueOrDefault(),
            State = evaluation.Answers.FirstOrDefault(a => a.QuestionId == 89331)?.AnswerValue,
            MemberPlanId = evaluation.MemberPlanId,
            MemberFirstName = evaluation.Answers.FirstOrDefault(a => a.QuestionId == 89325)?.AnswerValue,
            MemberLastName = evaluation.Answers.FirstOrDefault(a => a.QuestionId == 89326)?.AnswerValue,
            MemberGender = evaluation.Answers.FirstOrDefault(a => a.QuestionId == 90703)?.AnswerValue,
            MemberBirthDate = Convert.ToDateTime(evaluation.Answers.FirstOrDefault(a => a.QuestionId == 89327)?.AnswerValue),
            ProviderFirstName = provider.Content.FirstName,
            ProviderLastName = provider.Content.LastName,
            ProviderEmail = provider.Content.PersonalEmail,
            ProviderNpi = provider.Content.NationalProviderIdentifier,
            ProviderId = evaluation.ProviderId.ToString(),
            Images = evaluation.Answers
                .Where(a => a.QuestionId == 90650)
                .Select(i => i.AnswerValue)
                .ToList(),
            Answers = evaluation.Answers,
            RetinalImageTestingNotes = evaluation.Answers
                .FirstOrDefault(a => a.AnswerId == 50415)?.AnswerValue,
            HasEnucleation = evaluation.Answers.Any(a => a.AnswerId == 52927) ? true : null            
            
        };

        return answers;
    }
}