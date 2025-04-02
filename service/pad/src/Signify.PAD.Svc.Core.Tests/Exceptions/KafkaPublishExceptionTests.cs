using Signify.PAD.Svc.Core.Exceptions;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Exceptions;

public class KafkaPublishExceptionTests
{
    [Fact]
    public void Constructor_SetsProperties_Test()
    {
        const string message = "Error has occured";

        var ex = new KafkaPublishException(message);

        Assert.Equal(message, ex.Message);
    }
}