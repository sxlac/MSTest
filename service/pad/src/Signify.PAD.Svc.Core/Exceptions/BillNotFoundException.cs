using System;

namespace Signify.PAD.Svc.Core.Exceptions;

/// <summary>
/// Exception raised when no bill id is present in the BillRequestSent table
/// </summary>
[Serializable]
public class BillNotFoundException(long evaluationId, Guid billId)
    : Exception($"Unable to find a bill with EvaluationId={evaluationId}, for RCMBillId={billId}")
{
    public Guid RcmBillId { get; } = billId;

    public long EvaluationId { get; } = evaluationId;
}