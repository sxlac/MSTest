using System;

namespace Signify.uACR.Core.Exceptions;

[Serializable]
public class RequiredEvaluationQuestionMissingException(long evaluationId, int formVersionId, int questionId)
    : Exception(
        $"EvaluationID:{evaluationId} with FormVersionId:{formVersionId} and QuestionId:{questionId} is required but was missing")
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
    /// Identifier of the corresponding missing question
    /// </summary>
    public int QuestionId { get; } = questionId;
}