using System;
using System.Collections.Generic;
using System.Linq;
using Signify.CKD.Svc.Core.ApiClient.Response;

namespace Signify.CKD.Svc.Core.Queries.Helpers
{
    /// <summary>
    /// Builds a lookup of answers by QuestionId in a dictionary to decrease time complexity.
    /// In reality, there are normally (as of today) 500-1k questions that are answered for
    /// each and every evaluation in production, so enumerating the entire collection numerous
    /// times in search for each answer is not efficient.
    /// </summary>
    public class Lookup
    {
        public IReadOnlyDictionary<int, ICollection<EvaluationAnswerModel>> AnswersByQuestionId { get; }

        public Lookup(IEnumerable<EvaluationAnswerModel> answers)
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
            AnswersByQuestionId = dict;
        }

        public bool HasQuestion(int questionId, Func<EvaluationAnswerModel, bool> filter)
         => HasQuestion(questionId, filter, out _);

        public bool HasQuestion(int questionId, out IEnumerable<EvaluationAnswerModel> answers)
            => HasQuestion(questionId, null, out answers);

        public bool HasQuestion(int questionId, Func<EvaluationAnswerModel, bool> filter,
          out IEnumerable<EvaluationAnswerModel> answers)
        {
            if (AnswersByQuestionId.TryGetValue(questionId, out var answerModels))
            {
                answers = filter == null ? answerModels.OrderBy(ans => ans.AnswerId) : answerModels.Where(filter);
                if (answers == null || !answers.Any()) return false;
                return true;
            }

            answers = null;
            return false;
        }
    }
}

