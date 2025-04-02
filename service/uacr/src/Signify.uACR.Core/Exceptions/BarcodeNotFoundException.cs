using System;

namespace Signify.uACR.Core.Exceptions;

[Serializable]
public class BarcodeNotFoundException(
    long evaluationId,
    int formVersionId,
    int questionId,
    int answerId,
    long providerId)
    : Exception(
        $"Invalid answer value format for EvaluationId:{evaluationId}, FormVersionId:{formVersionId}, QuestionId:{questionId}, AnswerId:{answerId}, ProviderId:{providerId}")
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
    /// Identifier of the provider that corresponds with the evaluation
    /// </summary>
    public long ProviderId { get; } = providerId;
}
