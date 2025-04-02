using System;
using Signify.uACR.Core.Exceptions;
using Xunit;

namespace Signify.uACR.Core.Tests.Exceptions;

public class FhirParseExceptionTests
{
    [Fact]
    public void FhirParseException_ShouldContainInnerException()
    {
        // Arrange
        var innerException = new Exception("Inner exception message");
        // Act
        var exception = new FhirParseException("Failure", 1, "Vendor", 2, innerException);
        // Assert
        Assert.Equal("Failure EvaluationId: 1, Vendor Name: Vendor, LabResultId: 2", exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void FhirParseException_ShouldOutputErrorMessage()
    {
        // Arrange
        const string message = "Failed";
        // Act
        var exception = new FhirParseException(message);
        // Assert
        Assert.Equal("Failed", exception.Message);
    }

    [Fact]
    public void FhirParseException_ShouldSetMessageAndEvaluationId()
    {
        // Arrange
        const string message = "Error occurred";
        const long evaluationId = 123;
        // Act
        var exception = new FhirParseException(message, evaluationId);
        // Assert
        Assert.Equal("Error occurred EvaluationId: 123", exception.Message);
        Assert.Equal(evaluationId, exception.EvaluationId);
    }

    [Fact]
    public void FhirParseException_ShouldSetMessageVendorNameAndLabResultId()
    {
        // Arrange
        const string message = "Error occurred";
        const string vendorName = "Vendor";
        const long labResultId = 456;
        // Act
        var exception = new FhirParseException(message, vendorName, labResultId, null);
        // Assert
        Assert.Equal("Error occurred Vendor Name: Vendor, LabResultId: 456", exception.Message);
        Assert.Equal(vendorName, exception.VendorName);
    }
}