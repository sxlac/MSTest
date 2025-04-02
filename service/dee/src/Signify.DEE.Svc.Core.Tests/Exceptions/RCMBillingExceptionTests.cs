using System;
using Signify.DEE.Svc.Core.Exceptions;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Exceptions;

public class RCMBillingExceptionTests
{
    [Fact]
    public void Constructor_SetsMessage()
    {
        const string message = "Error";

        var ex = new RcmBillingException(message);

        Assert.Equal($"{message}", ex.Message);
    }

    [Fact]
    public void Constructor_SetsMessageWithException()
    {
        const string message = "Error";

        var ex = new RcmBillingException(new Exception(), message);

        Assert.Equal($"{message}", ex.Message);
    }
}