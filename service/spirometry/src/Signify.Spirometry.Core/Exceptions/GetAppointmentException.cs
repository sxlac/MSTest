using Refit;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.Serialization;

namespace Signify.Spirometry.Core.Exceptions
{
    /// <summary>
    /// Exception raised if there was an issue retrieving an appointment from the Scheduling API
    /// </summary>
    [Serializable]
    public sealed class GetAppointmentException : Exception
    {
        /// <summary>
        /// Identifier of the appointment
        /// </summary>
        public long AppointmentId { get; }

        /// <summary>
        /// HTTP status code received from the Scheduling API
        /// </summary>
        public HttpStatusCode StatusCode { get; }

        public GetAppointmentException(long appointmentId, HttpStatusCode statusCode, string message, ApiException innerException = null)
            : base($"{message} for AppointmentId={appointmentId}, with StatusCode={statusCode}", innerException)
        {
            AppointmentId = appointmentId;
            StatusCode = statusCode;
        }

        [ExcludeFromCodeCoverage]
        #region ISerializable
        [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        private GetAppointmentException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
        #endregion ISerializable
    }
}