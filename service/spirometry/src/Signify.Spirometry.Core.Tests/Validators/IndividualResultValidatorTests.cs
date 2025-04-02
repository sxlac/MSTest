using FakeItEasy;
using Signify.Spirometry.Core.Configs.Exam;
using Signify.Spirometry.Core.Validators;
using System;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Validators;

public class IndividualResultValidatorTests
{
    /// <summary>
    /// The subject under test is an abstract class; this is a dummy implementation of the abstract class for testing
    /// </summary>
    private class ConcreteDummy : IndividualResultValidator
    {
        public ConcreteDummy(IIntValueRangeConfig config)
            : base(config) { }
    }

    [Fact]
    public void Constructor_ValidatesValues_Test()
    {
        // Arrange
        var config = A.Fake<IIntValueRangeConfig>();

        A.CallTo(() => config.MinValueInclusive)
            .Returns(1);
        A.CallTo(() => config.MaxValueInclusive)
            .Returns(0);

        // Act
        // Assert
        Assert.ThrowsAny<ArgumentException>(() => new ConcreteDummy(config));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(4, true)]
    [InlineData(5, false)]
    public void IsValid_Range_Tests(int? value, bool expectedResult)
    {
        // Arrange
        var config = A.Fake<IIntValueRangeConfig>();

        A.CallTo(() => config.MinValueInclusive)
            .Returns(2);
        A.CallTo(() => config.MaxValueInclusive)
            .Returns(4);

        // Act
        var actual = new ConcreteDummy(config).IsValid(value);

        // Assert
        Assert.Equal(expectedResult, actual);
    }

    [Theory]
    [InlineData(null, false, null)]
    [InlineData("", false, null)]
    [InlineData(" ", false, null)]
    [InlineData("1.1", false, null)]
    [InlineData("10", false, 10)] // Integer, but out of range
    [InlineData("3", true, 3)]
    public void IsValid_OutValidatedResult_Tests(string rawValue, bool expectedResult, int? expectedValidatedResult)
    {
        // Arrange
        var config = A.Fake<IIntValueRangeConfig>();

        A.CallTo(() => config.MinValueInclusive)
            .Returns(2);
        A.CallTo(() => config.MaxValueInclusive)
            .Returns(4);

        // Act
        var actual = new ConcreteDummy(config).IsValid(rawValue, out var actualValidatedResult);

        // Assert
        Assert.Equal(expectedResult, actual);
        Assert.Equal(expectedValidatedResult, actualValidatedResult);
    }
}