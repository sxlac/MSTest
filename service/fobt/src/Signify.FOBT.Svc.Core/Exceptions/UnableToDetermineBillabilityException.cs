using System;

namespace Signify.FOBT.Svc.Core.Exceptions
{
    /// <summary>
    /// Exception raised when an event occurs that could trigger a billable event, but was unable to determine whether
    /// or not the event is billable
    /// </summary>
    public class UnableToDetermineBillabilityException : Exception
    {
        /// <summary>
        /// Corresponding evaluation's identifier
        /// </summary>
        public long EvaluationId { get; }

        public UnableToDetermineBillabilityException(long evaluationId)
            : this(evaluationId, $"Insufficient information known about evaluation to determine billability, for EvaluationId={evaluationId}")
        {
            
        }

        public UnableToDetermineBillabilityException(long evaluationId, string message)
            : base(message)
        {
            EvaluationId = evaluationId;
        }
    }
}
