using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Signify.Spirometry.Core.Exceptions
{
    /// <summary>
    /// Exception raised if we are unable to find an evaluation associated with
    /// a given appointment
    /// </summary>
    [Serializable]
    public sealed class EvaluationNotFoundException : Exception
    {
        /// <summary>
        /// Identifier of the appointment
        /// </summary>
        public long AppointmentId { get; set; }

        public EvaluationNotFoundException(long appointmentId)
            : base($"Unable to find an evaluation for AppointmentId={appointmentId}")
        {
            AppointmentId = appointmentId;
        }

        [ExcludeFromCodeCoverage]
        #region ISerializable
        [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        private EvaluationNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
        #endregion ISerializable
    }
}
