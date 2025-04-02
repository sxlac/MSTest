using FakeItEasy;
using Signify.Spirometry.Core.Converters;
using Signify.Spirometry.Core.Models;
using Signify.Spirometry.Core.Validators;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Converters;

public class Fev1NormalityConverterTests
{
    private readonly IFev1Validator _validator = A.Fake<IFev1Validator>();

    private readonly Fev1NormalityConverter _subject;

    public Fev1NormalityConverterTests()
    {
        _subject = new Fev1NormalityConverter(_validator);
    }

    [Theory]
    [MemberData(nameof(Convert_WithInvalidResult_TestData))]
    public void Convert_WithInvalidResult_Tests(int? result)
    {
        A.CallTo(() => _validator.IsValid(A<int?>._))
            .Returns(false);

        var actual = _subject.Convert(result);

        A.CallTo(() => _validator.IsValid(A<int?>.That.Matches(i => i == result)))
            .MustHaveHappened();

        Assert.Equal(NormalityIndicator.Undetermined, actual);
    }

    public static IEnumerable<object[]> Convert_WithInvalidResult_TestData()
    {
        // Reuse the other test data, but we only care about the first param (int?)
        return Convert_WithValidResult_TestData()
            .Select(resultSet => new[] { resultSet[0] });
    }

    [Theory]
    [MemberData(nameof(Convert_WithValidResult_TestData))]
    public void Convert_WithValidResult_Tests(int? result, NormalityIndicator expectedNormality)
    {
        A.CallTo(() => _validator.IsValid(A<int?>._))
            .Returns(true);

        var actual = _subject.Convert(result);

        A.CallTo(() => _validator.IsValid(A<int?>.That.Matches(i => i == result)))
            .MustHaveHappened();

        Assert.Equal(expectedNormality, actual);
    }

    public static IEnumerable<object[]> Convert_WithValidResult_TestData()
    {
        yield return [0, NormalityIndicator.Abnormal];
        yield return [1, NormalityIndicator.Abnormal];
        yield return [79, NormalityIndicator.Abnormal];
        yield return [80, NormalityIndicator.Normal];
        yield return [81, NormalityIndicator.Normal];
        yield return [125, NormalityIndicator.Normal];
    }
}