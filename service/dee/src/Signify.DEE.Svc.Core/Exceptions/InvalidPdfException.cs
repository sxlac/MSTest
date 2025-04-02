using System;

namespace Signify.DEE.Svc.Core.Exceptions;

/// <summary>
/// Exception raised when Pdf Base 64 string was not successfully recognized as valid Pdf
/// </summary>
[Serializable]
public class InvalidPdfException(long evaluationId, string message) : Exception(message)
{
    /// <summary>
    /// Corresponding evaluation's identifier
    /// </summary>
    public long EvaluationId { get; } = evaluationId;

    public InvalidPdfException(long evaluationId)
        : this(evaluationId, $"Invalid PDF base64 string received, for EvaluationId={evaluationId}, unable to convert it to a valid PDF")
    {
    }
}