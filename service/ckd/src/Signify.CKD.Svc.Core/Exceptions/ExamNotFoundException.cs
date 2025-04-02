using System;
using System.Runtime.Serialization;

namespace Signify.CKD.Svc.Core.Exceptions;

/// <summary>
/// Exception raised if there was no CKD exam data is found in CKD table
/// </summary>
[Serializable]
public class ExamNotFoundException : Exception
{
    public Guid EventId { get; }

    public long EvaluationId { get; }

    public ExamNotFoundException(long evaluationId, Guid eventId)
        : base($"Unable to find an exam with EvaluationId={evaluationId}, for EventId={eventId}")
    {
        EvaluationId = evaluationId;
        EventId = eventId;
    }

    #region ISerializable

    protected ExamNotFoundException(SerializationInfo info, StreamingContext context)
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