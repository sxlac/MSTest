using System;

namespace Signify.CKD.Svc.Core.Data.Entities
{
    /// <summary>
    /// Details explaining why a CKD exam that a provider was scheduled to perform was not
    /// actually performed
    /// </summary>
    public class ExamNotPerformed
    {
        /// <summary>
        /// Identifier of this record
        /// </summary>
        public int ExamNotPerformedId { get; set; }
        /// <summary>
        /// Foreign key identifier of the corresponding <see cref="CKD"/>
        /// </summary>
        public int CKDId { get; set; }
        /// <summary>
        /// Foreign key identifier of the corresponding <see cref="NotPerformedReason"/>
        /// </summary>
        public short NotPerformedReasonId { get; set; }
        /// <summary>
        /// Date and time this record was created
        /// </summary>
        public DateTime CreatedDateTime { get; set; }

        /// <summary>
        /// Gets or sets the Reason Notes for not performed
        /// </summary>
        public string Notes { get; set; }
    }
}
