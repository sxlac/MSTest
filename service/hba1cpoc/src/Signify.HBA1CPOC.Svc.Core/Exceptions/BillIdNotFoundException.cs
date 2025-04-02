using System;

namespace Signify.HBA1CPOC.Svc.Core.Exceptions;

/// <summary>
/// Exception raised when no bill id is present in the billing table
/// </summary>
[Serializable]
public class BillIdNotFoundException(long evaluationId, Guid billId)
    : Exception($"Unable to find a bill id with EvaluationId={evaluationId}, for RCMBillId={billId}")
{
    public Guid RcmBillId { get; } = billId;

    public long EvaluationId { get; } = evaluationId;
}