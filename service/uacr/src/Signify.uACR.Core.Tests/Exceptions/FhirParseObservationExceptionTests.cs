using Signify.uACR.Core.Exceptions;
using Xunit;

namespace Signify.uACR.Core.Tests.Exceptions;

public class FhirParseObservationExceptionTests
{
    [Fact]
    public void Constructor_WithMessageAndEvaluationId_SetsProperties()
    {
        // Arrange
        var message = "Observation: Invalid units:mg";
        var expectedEvaluationId = 1234567;
        var expectedMessage = "Observation: Invalid units:mg EvaluationId: "+1234567;

        // Act
        var exception = new FhirParseObservationException(message, expectedEvaluationId);

        // Assert
        Assert.Equal(expectedMessage, exception.Message);
        Assert.Equal(expectedEvaluationId, exception.EvaluationId);
    }
}