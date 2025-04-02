using Signify.Spirometry.Core.Exceptions;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Exceptions;

public class KafkaPublishExceptionTests
{
    [Fact]
    public void Constructor_SetsMessage_Test()
    {
        string message = "Error has occured";
        var ex = new KafkaPublishException(message);

        Assert.Equal(message, ex.Message);
    }
}