using Refit;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.Serialization;

namespace Signify.Spirometry.Core.Exceptions
{
    /// <summary>
    /// Exception raised if there was an issue retrieving evaluations for a given MemberPlanId
    /// from the Evaluation API
    /// </summary>
    [Serializable]
    public sealed class GetEvaluationsException : Exception
    {
        /// <summary>
        /// Identifier of the member plan sent in the request
        /// </summary>
        public long MemberPlanId { get; }

        /// <summary>
        /// Identifier of the appointment the <see cref="MemberPlanId"/> was associated with
        /// </summary>
        public long AppointmentId { get; }

        /// <summary>
        /// HTTP status code received from the Evaluation API
        /// </summary>
        public HttpStatusCode StatusCode { get; }

        public GetEvaluationsException(long memberPlanId, long appointmentId, HttpStatusCode statusCode, string message, ApiException innerException = null)
            : base($"{message} for MemberPlanId={memberPlanId}, from AppointmentId={appointmentId}, with StatusCode={statusCode}", innerException)
        {
            MemberPlanId = memberPlanId;
            AppointmentId = appointmentId;
            StatusCode = statusCode;
        }

        [ExcludeFromCodeCoverage]
        #region ISerializable
        [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        private GetEvaluationsException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
        #endregion ISerializable
    }
}
