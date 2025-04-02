using Signify.eGFR.Core.Exceptions;
using Xunit;

namespace Signify.eGFR.Core.Tests.Exceptions;

public class FhirParsePatientExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_SetsMessageProperty()
    {
        // Arrange
        var expectedMessage = "Patient: No Patient found in the DiagnosticReport.";

        // Act
        var exception = new FhirParsePatientException(expectedMessage);

        // Assert
        Assert.Equal(expectedMessage, exception.Message);
    }
}