using Signify.PAD.Svc.Core.Validators;
using System;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Validators;

public class DateTimeValidatorTests
{
    private readonly DateTimeValidator _validator = new();

    [Fact]
    public void IsValid_WithValidDateString_ReturnsTrueAndSetsValidatedResult()
    {
        // Arrange
        const string rawValue = "2023-10-01";

        // Act
        var result = _validator.IsValid(rawValue, out var validatedResult);

        // Assert
        Assert.True(result);
        Assert.Equal(DateTime.Parse(rawValue), validatedResult);
    }

    [Fact]
    public void IsValid_WithInvalidDateString_ReturnsFalseAndSetsValidatedResultToNull()
    {
        // Arrange
        const string rawValue = "invalid-date";

        // Act
        var result = _validator.IsValid(rawValue, out var validatedResult);

        // Assert
        Assert.False(result);
        Assert.Null(validatedResult);
    }

    [Fact]
    public void IsValid_WithValidDateString_ReturnsTrue()
    {
        // Arrange
        const string rawValue = "2023-10-01";

        // Act
        var result = _validator.IsValid(rawValue);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValid_WithInvalidDateString_ReturnsFalse()
    {
        // Arrange
        const string rawValue = "invalid-date";

        // Act
        var result = _validator.IsValid(rawValue);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WithValidDateStringAndFormat_ReturnsTrue()
    {
        // Arrange
        const string rawValue = "01-10-2023";
        const string format = "dd-MM-yyyy";

        // Act
        var result = _validator.IsValid(rawValue, format);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValid_WithInvalidDateStringAndFormat_ReturnsFalse()
    {
        // Arrange
        const string rawValue = "invalid-date";
        const string format = "dd-MM-yyyy";

        // Act
        var result = _validator.IsValid(rawValue, format);

        // Assert
        Assert.False(result);
    }
}