namespace Signify.CKD.Svc.Core.Data.Entities
{
    /// <summary>
    /// A reason why a provider may not have performed a CKD exam during an evaluation that included a CKD product
    /// </summary>
    public class NotPerformedReason
    {
        /// <summary>
        /// Identifier of this reason
        /// </summary>
        public short NotPerformedReasonId { get; set; }
        /// <summary>
        /// Identifier of the corresponding evaluation answer
        /// </summary>
        public int AnswerId { get; set; }
        /// <summary>
        /// Descriptive reason for why the exam was not performed
        /// </summary>
        public string Reason { get; set; }
    }
}
