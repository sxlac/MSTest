using System;

namespace Signify.eGFR.Core.Exceptions;

[Serializable]
public class UnsupportedAnswerForQuestionException(
    long evaluationId,
    int formVersionId,
    int questionId,
    int answerId,
    string answerValue,
    long providerId)
    : Exception(
        $"EvaluationID:{evaluationId} with FormVersionId:{formVersionId} and QuestionId:{questionId} has an unsupported AnswerId:{answerId}, with AnswerValue:{answerValue}, with ProviderId:{providerId}")
{
        
    /// <summary>
    /// Corresponding evaluation's identifier
    /// </summary>
    public long EvaluationId { get; } = evaluationId;

    /// <summary>
    /// Version of the Form this evaluation corresponds to.
    /// </summary>
    public int FormVersionId { get; } = formVersionId;

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

    /// <summary>
    /// Identifier of the provider that corresponds with the evaluation
    /// </summary>
    public long ProviderId { get; } = providerId;
}