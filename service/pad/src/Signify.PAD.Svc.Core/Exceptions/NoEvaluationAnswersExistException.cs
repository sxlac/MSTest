using System;

namespace Signify.PAD.Svc.Core.Exceptions;

[Serializable]
public class NoEvaluationAnswersExistException(long evaluationId)
    : Exception($"Evaluation API didn't return any answers for EvaluationId {evaluationId}")
{
    /// <summary>
    /// Identifier of the evaluation in question
    /// </summary>
    public long EvaluationId { get; } = evaluationId;
}