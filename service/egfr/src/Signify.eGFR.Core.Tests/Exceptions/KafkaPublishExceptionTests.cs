using System;
using Signify.eGFR.Core.Exceptions;
using Xunit;

namespace Signify.eGFR.Core.Tests.Exceptions;

public class KafkaPublishExceptionTests
{
    [Fact]
    public void Constructor_SetsMessage_Test()
    {
        const string message = "Testing123";

        var ex = new KafkaPublishException(message);

        Assert.Equal(message, ex.Message);
    }

    [Fact]
    public void Constructor_SetsMessageAndException_Test()
    {
        var exception = new Exception();
        const string message = "Testing123";

        var ex = new KafkaPublishException(exception, message);

        Assert.Equal(message, ex.Message);
    }
}