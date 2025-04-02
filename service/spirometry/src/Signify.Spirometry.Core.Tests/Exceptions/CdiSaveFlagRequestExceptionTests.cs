using Signify.Spirometry.Core.Exceptions;
using System.Net;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Exceptions;

public class CdiSaveFlagRequestExceptionTests
{
    [Fact]
    public void Constructor_SetsProperties_Test()
    {
        // Arrange
        const long evaluationId = 1;
        const HttpStatusCode statusCode = HttpStatusCode.UnavailableForLegalReasons;

        // Act
        var ex = new CdiSaveFlagRequestException(evaluationId, statusCode, default);

        // Assert
        Assert.Equal(evaluationId, ex.EvaluationId);
        Assert.Equal(statusCode, ex.StatusCode);
    }

    [Fact]
    public void Constructor_SetsMessage_Test()
    {
        // Arrange
        const long evaluationId = 1;
        const HttpStatusCode statusCode = HttpStatusCode.UnavailableForLegalReasons;
        const string message = "some message";

        // Act
        var ex = new CdiSaveFlagRequestException(evaluationId, statusCode, message);

        // Assert
        Assert.Equal("some message for EvaluationId=1, with StatusCode=UnavailableForLegalReasons", ex.Message);
    }
}