using NServiceBus;
using System;

namespace Signify.CKD.Svc.Core.Events
{
    public class ExamNotPerformedEvent : IMessage
    {
        public Guid EventId { get; set; }

        public Data.Entities.CKD Exam { get; set; }

        public short NotPerformedReasonId { get; set; }

        public string NotPerformedReasonNotes { get; set; }
    }
}
