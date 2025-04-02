using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Signify.DEE.Svc.Core.Exceptions
{
    [Serializable]
    public sealed class HoldNotFoundException : Exception
    {
        public Guid CdiHoldId { get; }

        public HoldNotFoundException(Guid cdiHoldId)
            : base($"Unable to find a hold with CdiHoldId={cdiHoldId}")
        {
            CdiHoldId = cdiHoldId;
        }

        [ExcludeFromCodeCoverage]
        #region ISerializable
        [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        private HoldNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
        #endregion ISerializable
    }
}
