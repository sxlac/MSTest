using System;

namespace Signify.eGFR.Core.Exceptions;
[Serializable]
public class DuplicateBarcodeException : Exception
{
    public long EvaluationId { get; }
    public string Barcode { get; }

    /// <summary>
    /// Exam not found in DB
    /// </summary>
    /// <param name="evaluationId"></param>
    /// <param name="barcode"></param>
    /// <param name="innerException"></param>
    public DuplicateBarcodeException(long evaluationId, string barcode,  Exception innerException = null)
        : base($"EvaluationId :{evaluationId} with Barcode:{barcode} already exists in DB", innerException)
    {
        EvaluationId = evaluationId;
        Barcode = barcode;
    }
}