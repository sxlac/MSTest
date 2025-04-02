using System;

namespace Signify.eGFR.Core.Exceptions;

[Serializable]
public class LabResultNotFoundException : Exception
{
    public long EvaluationId { get; }
    
    /// <summary>
    /// LabResult not found in DB
    /// </summary>
    /// <param name="evaluationId"></param>
    public LabResultNotFoundException(long evaluationId)
        : base($"LabResult with EvaluationId:{evaluationId} not found in DB")
    {
        EvaluationId = evaluationId;
    }
}