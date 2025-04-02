using Signify.eGFR.Core.Exceptions;
using Xunit;

namespace Signify.eGFR.Core.Tests.Exceptions;

public class FhirParseDiagnosticReportExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_SetsMessageProperty()
    {
        // Arrange
        var expectedMessage = "DiagnosticReport: No DiagnosticReport found in the Bundle.";

        // Act
        var exception = new FhirParseDiagnosticReportException(expectedMessage);

        // Assert
        Assert.Equal(expectedMessage, exception.Message);
    }
}