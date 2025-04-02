using System;
using System.Runtime.Serialization;

namespace Signify.CKD.Svc.Core.Exceptions;

[Serializable]
public class ExamWithStatusNotFoundException : Exception
{
    public long EvaluationId { get; }
    public Guid EventId { get; }
    public string Status { get; }

    /// <summary>
    /// Exam not found in DB
    /// </summary>
    /// <param name="evaluationId"></param>
    /// <param name="eventId"></param>
    public ExamWithStatusNotFoundException(long evaluationId, Guid eventId, string status)
        : base($"Exam with EvaluationId={evaluationId} and {status} not found in DB. EventId={eventId}.")
    {
        EvaluationId = evaluationId;
        EventId = eventId;
        Status = status;
    }
    
    #region ISerializable
    protected ExamWithStatusNotFoundException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        EvaluationId = info.GetInt64("EvaluationId");
        EventId = Guid.Parse(info.GetString("EventId")!);
        Status = info.GetString("Status")!;
    }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);

        info.AddValue("EvaluationId", EvaluationId.ToString(), typeof(string));
        info.AddValue("EventId", EventId, typeof(Guid));
        info.AddValue("Status", Status, typeof(string));
    }
    #endregion ISerializable
}