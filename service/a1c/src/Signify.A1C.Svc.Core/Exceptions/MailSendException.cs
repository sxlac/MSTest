using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.A1C.Svc.Core.Exceptions
{
    [ExcludeFromCodeCoverage]
    [Serializable]
    public class MailSendException : Exception
    {
        public MailSendException(string message = "") : base(message) { }
        public MailSendException(Exception ex, string message = "") : base(message, ex) { }
    }
}