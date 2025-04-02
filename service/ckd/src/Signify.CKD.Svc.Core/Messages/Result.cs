using System;
using System.Collections.Generic;

namespace Signify.CKD.Svc.Core.Messages
{
    /// <summary>
    /// Result details published to Kafka
    /// </summary>
    public class Result
    {
        /// <summary>
        /// Signify Product Code for this event
        /// </summary>
        public string ProductCode { get; set; }

        /// <summary>
        /// Unique identifier of the evaluation this event corresponds to
        /// </summary>
        public long EvaluationId { get; set; }

        /// <summary>
        /// UTC timestamp this evaluation was finalized on the provider's iPad (not necessarily when the Signify
        /// Evaluation API received the message, for ex in the case of the iPad being offline)
        /// </summary>
        public DateTimeOffset? PerformedDate { get; set; }

        /// <summary>
        /// UTC timestamp results for this product and evaluation were received by this process manager
        /// </summary>
        public DateTimeOffset ReceivedDate { get; set; }

        public Boolean IsBillable { get; set; }

        public string Determination { get; set; } = "U";

        public DateTimeOffset? ExpiryDate { get; set; }

        public ICollection<ResultType> Results { get; set; } = new List<ResultType>();
    }

    public class ResultType
    {
        public ResultType(string type, string result, string resultUnit, string severity) 
        { 
            Type = type; 
            Result = result; 
            ResultUnit = resultUnit; 
            Severity = severity; 
        }

        public string Type { get; set; }
        public string Result { get; set; }
        public string ResultUnit { get; set; }
        public string Severity { get; set; }
    }

    enum ResultTypeName
    {
        uAcr,
        Albumin,
        Creatinine,
        Exception
    }

}
