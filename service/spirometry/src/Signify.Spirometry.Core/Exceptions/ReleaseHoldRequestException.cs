using Refit;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.Serialization;

namespace Signify.Spirometry.Core.Exceptions
{
    /// <summary>
    /// Exception raised if there was an issue sending a request to the CDI Holds API to release a hold
    /// </summary>
    [Serializable]
    public sealed class ReleaseHoldRequestException : Exception
    {
        /// <summary>
        /// Corresponding evaluation's identifier
        /// </summary>
        public long EvaluationId { get; }

        /// <summary>
        /// Identifier of the hold in the context of the Spirometry process manager
        /// </summary>
        public int HoldId { get; }

        /// <summary>
        /// Identifier of the hold in the context of CDI
        /// </summary>
        public Guid CdiHoldId { get; }

        /// <summary>
        /// HTTP status code received from the CDI API
        /// </summary>
        public HttpStatusCode StatusCode { get; }

        /// <summary>
        /// The reason phrase which typically is sent by the API server together with
        /// the status code 
        /// </summary>
        public string ReasonPhrase { get; }

        /// <summary>
        /// HTTP response content
        /// </summary>
        public string ResponseContent { get; }

        public ReleaseHoldRequestException(long evaluationId, int holdId, Guid cdiHoldId, ApiException exception)
            : base($"Failed to release hold with CdiHoldId={cdiHoldId} for EvaluationId={evaluationId}, with StatusCode={exception.StatusCode}", exception)
        {
            EvaluationId = evaluationId;
            HoldId = holdId;
            CdiHoldId = cdiHoldId;
            StatusCode = exception.StatusCode;
            ReasonPhrase = exception.ReasonPhrase;
            ResponseContent = exception.Content;
        }

        [ExcludeFromCodeCoverage]
        #region ISerializable
        [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        private ReleaseHoldRequestException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
        #endregion ISerializable
    }
}
