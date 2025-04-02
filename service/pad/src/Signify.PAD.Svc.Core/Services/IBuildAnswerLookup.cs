using Signify.PAD.Svc.Core.ApiClient.Response;
using System.Collections.Generic;

namespace Signify.PAD.Svc.Core.Services
{
    public interface IBuildAnswerLookup
    {
        /// <summary>
        /// Builds a lookup of Questions to Answers
        /// </summary>
        /// <remarks>
        /// Key: QuestionId<br />
        /// Value: Collection of one or more answers to the given question
        /// </remarks>
        IReadOnlyDictionary<int, ICollection<EvaluationAnswerModel>> BuildLookup(IEnumerable<EvaluationAnswerModel> answers);
    }
}
