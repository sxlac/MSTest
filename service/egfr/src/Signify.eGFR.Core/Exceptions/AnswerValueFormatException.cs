using System;

namespace Signify.eGFR.Core.Exceptions;

[Serializable]
public class AnswerValueFormatException(
    long evaluationId,
    int formVersionId,
    int questionId,
    int answerId,
    string answerValue,
    long providerId)
    : Exception(
        $"Invalid answer value format for EvaluationId:{evaluationId}, FormVersionId:{formVersionId}, QuestionId:{questionId}, AnswerId:{answerId}, AnswerValue:{answerValue}, ProviderId:{providerId}")
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
    /// Identifier of the corresponding question
    /// </summary>
    public int QuestionId { get; } = questionId;

    /// <summary>
    /// Identifier of the answer that was found for this question
    /// </summary>
    public int AnswerId { get; } = answerId;

    /// <summary>
    /// Actual answer value that is in the wrong format
    /// </summary>
    public string AnswerValue { get; } = answerValue;

    /// <summary>
    /// Identifier of the provider that corresponds with the evaluation
    /// </summary>
    public long ProviderId { get; } = providerId;
}