using System;
using Signify.DEE.Svc.Core.Exceptions;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Exceptions;

public class KafkaPublishExceptionTests
{
    [Fact]
    public void Constructor_SetsCustomMessage()
    {
        const string message = "Error";

        var ex = new KafkaPublishException(message);

        Assert.Equal($"{message}", ex.Message);
    }

    [Fact]
    public void Constructor_SetsMessageWithException()
    {
        const string message = "Error";

        var ex = new KafkaPublishException(new Exception(), message);

        Assert.Equal($"{message}", ex.Message);
    }
}