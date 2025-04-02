using Refit;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.Serialization;

namespace Signify.Spirometry.Core.Exceptions
{
    /// <summary>
    /// Exception raised if there was an issue sending a billing request to the RCM API
    /// </summary>
    [Serializable]
    public sealed class RcmBillingRequestException : Exception
    {
        /// <summary>
        /// Identifier of the event that raised this exception
        /// </summary>
        public Guid EventId { get; }
        /// <summary>
        /// Corresponding evaluation's identifier
        /// </summary>
        public long EvaluationId { get; }
        /// <summary>
        /// HTTP status code received from the RCM API
        /// </summary>
        public HttpStatusCode StatusCode { get; }

        public RcmBillingRequestException(Guid eventId, long evaluationId, HttpStatusCode statusCode, string message, ApiException innerException = null)
            : base($"{message} for EventId={eventId}, EvaluationId={evaluationId}, with StatusCode={statusCode}", innerException)
        {
            EventId = eventId;
            EvaluationId = evaluationId;
            StatusCode = statusCode;
        }

        [ExcludeFromCodeCoverage]
        #region ISerializable
        [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        private RcmBillingRequestException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
        #endregion ISerializable
    }
}
