using System.Collections.Generic;
using Signify.uACR.Core.ApiClients.EvaluationApi.Responses;

namespace Signify.uACR.Core.Services;

public interface IBuildAnswerLookup
{
    /// <summary>
    /// Builds a lookup of Questions to Answers
    /// </summary>
    /// <remarks>
    /// Key: QuestionId
    /// Value: Collection of one or more answers to the given question
    /// </remarks>
    IReadOnlyDictionary<int, ICollection<EvaluationAnswerModel>> BuildLookup(IEnumerable<EvaluationAnswerModel> answers);
}