using Signify.PAD.Svc.Core.ApiClient.Response;
using System.Collections.Generic;

namespace Signify.PAD.Svc.Core.Services
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
                    dict[answer.QuestionId] = answerModels = new List<EvaluationAnswerModel>();

                answerModels.Add(answer);
            }
            return dict;
        }
    }
}
