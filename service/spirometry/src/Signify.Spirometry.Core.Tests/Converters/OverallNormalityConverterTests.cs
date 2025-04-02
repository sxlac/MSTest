using FakeItEasy;
using Signify.Spirometry.Core.Converters;
using Signify.Spirometry.Core.Models;
using Signify.Spirometry.Core.Services;
using Signify.Spirometry.Core.Validators;
using System.Collections.Generic;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Converters;

public class OverallNormalityConverterTests
{
    private readonly IFev1FvcRatioValidator _validator = A.Fake<IFev1FvcRatioValidator>();
    private readonly IExamQualityService _examQualityService = A.Fake<IExamQualityService>();

    private OverallNormalityConverter CreateSubject() => new(_validator, _examQualityService);

    [Fact]
    public void Convert_WithNormalRatio_ButInsufficientSessionGrade_ReturnsUndetermined()
    {
        var results = new ExamResult
        {
            Fev1FvcRatio = 0.75m // Value doesn't matter, as long as it is within normal range
        };

        A.CallTo(() => _examQualityService.IsSufficientQuality(A<ExamResult>._))
            .Returns(false);

        A.CallTo(() => _validator.IsValid(A<decimal>._))
            .Returns(true);

        var subject = CreateSubject();

        var actual = subject.Convert(results);

        Assert.Equal(NormalityIndicator.Undetermined, actual);
    }

    [Fact]
    public void Convert_WithNormalRatio_AndSufficientSessionGrade_ReturnsNormal()
    {
        var results = new ExamResult
        {
            Fev1FvcRatio = 0.75m // Value doesn't matter, as long as it is within normal range
        };

        A.CallTo(() => _examQualityService.IsSufficientQuality(A<ExamResult>._))
            .Returns(true);

        A.CallTo(() => _validator.IsValid(A<decimal>._))
            .Returns(true);

        var subject = CreateSubject();

        var actual = subject.Convert(results);

        Assert.Equal(NormalityIndicator.Normal, actual);
    }

    [Theory]
    [MemberData(nameof(Convert_WithSufficientSessionGrade_AndUndeterminedRatio_ReturnsUndetermined_TestData))]
    public void Convert_WithSufficientSessionGrade_AndUndeterminedRatio_ReturnsUndetermined(decimal? ratio, bool isValid)
    {
        var results = new ExamResult
        {
            Fev1FvcRatio = ratio
        };

        A.CallTo(() => _examQualityService.IsSufficientQuality(A<ExamResult>._))
            .Returns(true);

        A.CallTo(() => _validator.IsValid(A<decimal>._))
            .Returns(isValid);

        var subject = CreateSubject();

        var actual = subject.Convert(results);

        Assert.Equal(NormalityIndicator.Undetermined, actual);
    }

    public static IEnumerable<object[]> Convert_WithSufficientSessionGrade_AndUndeterminedRatio_ReturnsUndetermined_TestData()
    {
        yield return [null, false];
        yield return [null, true];
        yield return [0.75m, false];
    }

    [Theory]
    [InlineData(-1, NormalityIndicator.Abnormal)]
    [InlineData(0, NormalityIndicator.Abnormal)]
    [InlineData(0.1, NormalityIndicator.Abnormal)]
    [InlineData(0.699, NormalityIndicator.Abnormal)]
    [InlineData(0.7, NormalityIndicator.Normal)]
    [InlineData(0.75, NormalityIndicator.Normal)]
    [InlineData(0.8, NormalityIndicator.Normal)]
    [InlineData(1, NormalityIndicator.Normal)]
    [InlineData(10, NormalityIndicator.Normal)] // This isn't within valid range, but for this test, we are faking the validator to always return valid, so the subject shouldn't care
    public void Convert_WithSufficientSessionGrade_Ratio_Tests(decimal ratio, NormalityIndicator expected)
    {
        var results = new ExamResult
        {
            Fev1FvcRatio = ratio
        };

        A.CallTo(() => _examQualityService.IsSufficientQuality(A<ExamResult>._))
            .Returns(true);

        A.CallTo(() => _validator.IsValid(A<decimal>._))
            .Returns(true);

        var subject = CreateSubject();

        var actual = subject.Convert(results);

        Assert.Equal(expected, actual);
    }
}