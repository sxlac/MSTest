using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Signify.Spirometry.Core.Data.Entities
{
    /// <summary>
    /// Corresponds to the different status updates a Spirometry exam can go through
    /// </summary>
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global - Virtual properties are used by EF
    public class StatusCode : IEntityEnum<StatusCode>
    {
        public static readonly StatusCode SpirometryExamPerformed = new StatusCode(1, "Spirometry Exam Performed");
        public static readonly StatusCode SpirometryExamNotPerformed = new StatusCode(2, "Spirometry Exam Not Performed");
        public static readonly StatusCode BillableEventReceived = new StatusCode(3, "Billable Event Received");
        public static readonly StatusCode BillRequestSent = new StatusCode(4, "Bill Request Sent");
        public static readonly StatusCode ClientPdfDelivered = new StatusCode(5, "Client PDF Delivered");
        public static readonly StatusCode BillRequestNotSent = new StatusCode(6, "Bill Request Not Sent");
        public static readonly StatusCode OverreadProcessed = new StatusCode(7, "Overread Processed");
        public static readonly StatusCode ResultsReceived = new StatusCode(8, "Results Received");
        public static readonly StatusCode ClarificationFlagCreated = new StatusCode(9, "Clarification Flag Created");
        public static readonly StatusCode ProviderPayableEventReceived = new StatusCode(10, "Provider Payable Event Received");
        public static readonly StatusCode ProviderPayRequestSent = new StatusCode(11, "Provider Pay Request Sent");
        public static readonly StatusCode ProviderNonPayableEventReceived = new StatusCode(12, "Provider Non-Payable Event Received");
        public static readonly StatusCode CdiPassedReceived = new StatusCode(13, "CDI Passed Received");
        public static readonly StatusCode CdiFailedWithPayReceived = new StatusCode(14, "CDI Failed with Pay Received");
        public static readonly StatusCode CdiFailedWithoutPayReceived = new StatusCode(15, "CDI Failed without Pay Received");

        /// <inheritdoc />
        public IEnumerable<StatusCode> GetAllEnumerations()
            => new[]
            {
                SpirometryExamPerformed,
                SpirometryExamNotPerformed,
                BillableEventReceived,
                BillRequestSent,
                ClientPdfDelivered,
                BillRequestNotSent,
                OverreadProcessed,
                ResultsReceived,
                ClarificationFlagCreated,
                ProviderPayableEventReceived,
                ProviderPayRequestSent,
                ProviderNonPayableEventReceived,
                CdiPassedReceived,
                CdiFailedWithPayReceived,
                CdiFailedWithoutPayReceived
            };

        /// <summary>
        /// Identifier of this status code
        /// </summary>
        [Key]
        public int StatusCodeId { get; init; }

        /// <summary>
        /// Short name for the status code
        /// </summary>
        public string Name { get; }

        private StatusCode(int statusCodeId, string name)
        {
            StatusCodeId = statusCodeId;
            Name = name;
        }

        public virtual ICollection<ExamStatus> ExamStatuses { get; set; } = new HashSet<ExamStatus>();
    }
}
