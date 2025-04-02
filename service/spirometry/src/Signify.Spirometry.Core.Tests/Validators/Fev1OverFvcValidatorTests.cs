using Signify.Spirometry.Core.Validators;
using System.Collections.Generic;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Validators;

public class Fev1OverFvcValidatorTests
{
    [Theory]
    [MemberData(nameof(IsValid_FromRawValue_TestData))]
    public void IsValid_FromRawValue_Tests(string rawValue, decimal? expectedValidatedResult, bool expectedIsValid)
    {
        var subject = new Fev1FvcRatioValidator();

        Assert.Equal(expectedIsValid, subject.IsValid(rawValue, out var actualResult));

        Assert.Equal(expectedValidatedResult, actualResult);
    }

    // Not able to do these as InlineData because ones casted to (decimal?) break compilation because they must be constants
    public static IEnumerable<object[]> IsValid_FromRawValue_TestData()
    {
        yield return [null, null, false];
        yield return ["", null, false];
        yield return [" ", null, false];
        yield return ["invalid", null, false];
        yield return ["1a", null, false];
        yield return ["0", (decimal?)0, false];
        yield return ["0.0", (decimal?)0, false];
        yield return ["-1", (decimal?)-1, false];
        yield return ["1.01", (decimal?)1.01, false];
        yield return ["2", (decimal?)2, false];
        yield return ["0.01", (decimal?)0.01, true];
        yield return [".1", (decimal?)0.1, true];
        yield return ["0.1", (decimal?)0.1, true];
        yield return ["0.10", (decimal?)0.1, true];
        yield return [".50", (decimal?)0.5, true];
        yield return ["1", (decimal?)1, true];
        yield return ["1.00", (decimal?)1, true];
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(-0.75, false)]
    [InlineData(-0.0001, false)]
    [InlineData(0, false)]
    [InlineData(0.01, true)]
    [InlineData(0.00001, true)]
    [InlineData(0.1, true)]
    [InlineData(0.2, true)]
    [InlineData(0.5, true)]
    [InlineData(0.75, true)]
    [InlineData(0.9, true)]
    [InlineData(0.9999999, true)]
    [InlineData(1, true)]
    [InlineData(1.0001, false)]
    [InlineData(2, false)]
    [InlineData(10, false)]
    public void IsValid_FromDecimal_Tests(decimal ratio, bool expectedIsValid)
    {
        var subject = new Fev1FvcRatioValidator();

        var actual = subject.IsValid(ratio);

        Assert.Equal(expectedIsValid, actual);
    }
}