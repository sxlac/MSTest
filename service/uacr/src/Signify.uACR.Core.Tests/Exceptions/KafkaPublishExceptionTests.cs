using Signify.uACR.Core.Exceptions;
using Xunit;

namespace Signify.uACR.Core.Tests.Exceptions;

public class KafkaPublishExceptionTests
{
    [Fact]
    public void Constructor_SetsMessage_Test()
    {
        const string message = "Testing";

        var ex = new KafkaPublishException(message);

        Assert.Equal(message, ex.Message);
    }
}