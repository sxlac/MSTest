using System;

namespace Signify.DEE.Svc.Core.Exceptions;

public class RcmBillingException : Exception
{
    public RcmBillingException(string message) : base(message) { }
    public RcmBillingException(Exception ex, string message = "") : base(message, ex) { }
}