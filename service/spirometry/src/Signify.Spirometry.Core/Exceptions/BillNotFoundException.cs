using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Signify.Spirometry.Core.Exceptions;

/// <summary>
/// Exception raised when no bill id is present in the BillRequestSent table
/// </summary>
[Serializable]
public sealed class BillNotFoundException : Exception
{
    public Guid RcmBillId { get; }

    public long EvaluationId { get; }

    public BillNotFoundException(long evaluationId, Guid billId)
        : base($"Unable to find a bill with EvaluationId={evaluationId}, for RCMBillId={billId}")
    {
        EvaluationId = evaluationId;
        RcmBillId = billId;
    }

    [ExcludeFromCodeCoverage]
    #region ISerializable
    [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
    private BillNotFoundException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }
    #endregion ISerializable
}