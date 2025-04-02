using System;

namespace Signify.FOBT.Svc.Core.Exceptions;

/// <summary>
/// Exception raised when no bill id is present in the BillRequestSent table
/// </summary>
public class BillNotFoundException : Exception
{
    public Guid RcmBillId { get; }

    public long EvaluationId { get; }

    public BillNotFoundException(long evaluationId, Guid billId)
        : base($"Unable to find a bill with EvaluationId={evaluationId}, for RCMBillId={billId}")
    {
        EvaluationId = evaluationId;
        RcmBillId = billId;
    }
}