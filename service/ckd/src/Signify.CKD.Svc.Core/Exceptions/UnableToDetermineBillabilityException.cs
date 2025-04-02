using System;
using System.Runtime.Serialization;

namespace Signify.CKD.Svc.Core.Exceptions;

/// <summary>
/// Exception raised when an event occurs that could trigger a billable event, but was unable to determine whether
/// or not the event is billable
/// </summary>
[Serializable]
public class UnableToDetermineBillabilityException : Exception
{
    /// <summary>
    /// Identifier of the event that raised this exception
    /// </summary>
    public Guid EventId { get; }

    /// <summary>
    /// Corresponding evaluation's identifier
    /// </summary>
    public long EvaluationId { get; }

    public UnableToDetermineBillabilityException(Guid eventId, long evaluationId)
        : base(
            $"Insufficient information known about evaluation to determine billability, for EventId={eventId}, EvaluationId={evaluationId}")
    {
        EventId = eventId;
        EvaluationId = evaluationId;
    }

    #region ISerializable
    protected UnableToDetermineBillabilityException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        EventId = Guid.Parse(info.GetString("EventId")!);
        EvaluationId = info.GetInt64("EvaluationId");
    }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);

        info.AddValue("EventId", EventId.ToString(), typeof(string));
        info.AddValue("EvaluationId", EvaluationId, typeof(long));
    }
    #endregion ISerializable
}
