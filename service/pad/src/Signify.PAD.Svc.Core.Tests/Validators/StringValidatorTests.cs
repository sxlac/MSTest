using Signify.PAD.Svc.Core.Validators;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Validators;

public class StringValidatorTests
{
    private readonly StringValidator _validator = new();

    [Fact]
    public void IsValid_WithValidString_ReturnsTrueAndSetsValidatedResult()
    {
        // Arrange
        const string rawValue = "test";

        // Act
        var result = _validator.IsValid(rawValue, out var validatedResult);

        // Assert
        Assert.True(result);
        Assert.Equal(rawValue, validatedResult);
    }

    [Fact]
    public void IsValid_WithInvalidString_ReturnsFalseAndSetsValidatedResultToNull()
    {
        // Arrange
        const string rawValue = "";

        // Act
        var result = _validator.IsValid(rawValue, out var validatedResult);

        // Assert
        Assert.False(result);
        Assert.Null(validatedResult);
    }

    [Fact]
    public void IsValid_WithNonEmptyString_ReturnsTrue()
    {
        // Arrange
        const string rawValue = "test";

        // Act
        var result = _validator.IsValid(rawValue);

        // Assert
        Assert.True(result);
    }
    
    [Fact]
    public void IsValid_WithEmptyString_ReturnsFalse()
    {
        // Arrange
        const string rawValue = "";

        // Act
        var result = _validator.IsValid(rawValue);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WithValidStringAndDelimiter_ReturnsTrue()
    {
        // Arrange
        const string rawValue = "test,example";
        const string delimiter = ",";

        // Act
        var result = _validator.IsValid(rawValue, delimiter);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValid_ReturnsTrue()
    {
        // Arrange
        const string rawValue = "";
        const string delimiter = ",";

        // Act
        var result = _validator.IsValid(rawValue, delimiter);

        // Assert
        Assert.True(result);
    }
}