using System;
using System.Net;
using System.Runtime.Serialization;
using Refit;

namespace Signify.CKD.Svc.Core.Exceptions
{
    /// <summary>
    /// Exception raised if there was an issue sending a billing request to the RCM API
    /// </summary>
    [Serializable]
    public class RcmBillingRequestException : Exception
    {
        /// <summary>
        /// Corresponding evaluation's identifier
        /// </summary>
        public long EvaluationId { get; }
        /// <summary>
        /// HTTP status code received from the RCM API
        /// </summary>
        public HttpStatusCode StatusCode { get; }

        public RcmBillingRequestException(long evaluationId, HttpStatusCode statusCode, string message, ApiException innerException = null)
            : base($"{message} for EvaluationId={evaluationId}, with StatusCode={statusCode}", innerException)
        {
            EvaluationId = evaluationId;
            StatusCode = statusCode;
        }

        #region ISerializable
        protected RcmBillingRequestException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            EvaluationId = info.GetInt64("EvaluationId");
            StatusCode = (HttpStatusCode) info.GetInt32("StatusCode");
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue("EvaluationId", EvaluationId, typeof(long));
            info.AddValue("StatusCode", (int)StatusCode, typeof(int));
        }
        #endregion ISerializable
    }
}