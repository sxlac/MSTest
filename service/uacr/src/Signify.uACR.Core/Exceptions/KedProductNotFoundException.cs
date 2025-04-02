using System;

namespace Signify.uACR.Core.Exceptions;

[Serializable]
public class KedProductNotFoundException(long evaluationId, decimal? uacrResult, bool isBillable) : Exception(
    $"KED missing product code not found exception EvaluationId={evaluationId}, Result={uacrResult}, IsBillable={isBillable}")
{
    public long EvaluationId { get; } = evaluationId;

    public decimal? UacrResult { get; } = uacrResult;

    public bool IsBillable { get; } = isBillable;
}