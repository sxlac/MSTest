using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.DEE.Svc.Core.Data.Entities
{
    [ExcludeFromCodeCoverage]
    public class Hold
    {
        /// <summary>
        /// Identifier of this hold in the DEE database
        /// </summary>
        /// <remarks>
        /// Not to be mistaken for the hold's identifier outside of the DEE context, which
        /// is the <see cref="CdiHoldId"/>.
        /// </remarks>
        public int HoldId { get; set; }
        /// <summary>
        /// Identifier of this hold outside of the DEE context
        /// </summary>
        public Guid CdiHoldId { get; set; }
        /// <summary>
        /// Identifier of the evaluation this hold is applied to
        /// </summary>
        public int EvaluationId { get; set; }
        /// <summary>
        /// Default time this hold will expire if not released
        /// </summary>
        public DateTime ExpiresAt { get; set; }
        /// <summary>
        /// When this hold was created in CDI
        /// </summary>
        public DateTime HeldOnDateTime { get; set; }
        public DateTime SentAtDateTime { get; set; }
        /// <summary>
        /// When this hold was released
        /// </summary>
        public DateTime? ReleasedDateTime { get; set; }
        /// <summary>
        /// When the DEE process manager inserted this hold into it's database
        /// </summary>
        public DateTime CreatedDateTime { get; set; }
    }
}
