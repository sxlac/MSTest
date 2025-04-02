using System;

namespace Signify.PAD.Svc.Core.Exceptions;

[Serializable]
public class UnsupportedAnswerForQuestionException(long evaluationId, int questionId, int answerId, string answerValue)
    : Exception(
        $"QuestionId:{questionId} has an unsupported AnswerId:{answerId}, with AnswerValue:{answerValue}, for EvaluationId:{evaluationId}")
{
    /// <summary>
    /// Evaluation associated with this exception
    /// </summary>
    public long EvaluationId { get; } = evaluationId;

    /// <summary>
    /// Identifier of the question that has an answer selected that is not currently supported
    /// </summary>
    public int QuestionId { get; } = questionId;

    /// <summary>
    /// Identifier of the unsupported answer
    /// </summary>
    public int AnswerId { get; } = answerId;

    /// <summary>
    /// Answer value of the unsupported answer
    /// </summary>
    public string AnswerValue { get; } = answerValue;
}