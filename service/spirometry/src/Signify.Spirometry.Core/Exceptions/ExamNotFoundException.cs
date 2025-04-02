using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Signify.Spirometry.Core.Exceptions;

[Serializable]
public sealed class ExamNotFoundException : Exception
{
    public Guid EventId { get; }

    public long EvaluationId { get; }

    public ExamNotFoundException(long evaluationId, Guid eventId)
        : base($"Unable to find an exam with EvaluationId={evaluationId}, for EventId={eventId}")
    {
        EvaluationId = evaluationId;
        EventId = eventId;
    }

    [ExcludeFromCodeCoverage]
    #region ISerializable
    [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
    private ExamNotFoundException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }
    #endregion ISerializable
}