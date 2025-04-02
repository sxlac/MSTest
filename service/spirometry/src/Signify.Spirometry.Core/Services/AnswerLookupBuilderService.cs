using Signify.Spirometry.Core.ApiClients.EvaluationApi.Responses;
using System.Collections.Generic;

namespace Signify.Spirometry.Core.Services
{
    public class AnswerLookupBuilderService : IBuildAnswerLookup
    {
        /// <inheritdoc />
        public IReadOnlyDictionary<int, ICollection<EvaluationAnswerModel>> BuildLookup(IEnumerable<EvaluationAnswerModel> answers)
        {
            var dict = new Dictionary<int, ICollection<EvaluationAnswerModel>>();
            foreach (var answer in answers)
            {
                if (!dict.TryGetValue(answer.QuestionId, out var answerModels))
                {
                    answerModels = new List<EvaluationAnswerModel>();
                    dict.Add(answer.QuestionId, answerModels);
                }

                answerModels.Add(answer);
            }
            return dict;
        }
    }
}
