using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Signify.Spirometry.Core.Exceptions;

/// <summary>
/// Exception raised when an event occurs that could trigger a Payable event, but was unable to determine whether
/// the event is Payable
/// </summary>
[Serializable]
public sealed class UnableToDeterminePayableException : Exception
{
    /// <summary>
    /// Identifier of the event that raised this exception
    /// </summary>
    public Guid EventId { get; }

    /// <summary>
    /// Corresponding evaluation's identifier
    /// </summary>
    public long EvaluationId { get; }

    public UnableToDeterminePayableException(Guid eventId, long evaluationId)
        : base(
            $"Insufficient information known about evaluation to determine payable, for EventId={eventId}, EvaluationId={evaluationId}")
    {
        EventId = eventId;
        EvaluationId = evaluationId;
    }

    [ExcludeFromCodeCoverage]
    #region ISerializable
    [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
    private UnableToDeterminePayableException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }
    #endregion ISerializable
}