using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.Spirometry.Core.Configs.Loopback;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Services.Flags;
using System.Collections.Generic;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Services.Flags;

public class FlagTextFormatterTests
{
    [Theory]
    [MemberData(nameof(FormatFlagText_TestData))]
    public void FormatFlagText_Tests(string format, decimal? ratio, int lfq, string expected)
    {
        // Arrange
        var config = A.Fake<IGetLoopbackConfig>();

        A.CallTo(() => config.FlagTextFormat)
            .Returns(format);
        A.CallTo(() => config.HighRiskLfqScoreMaxValue)
            .Returns(lfq);

        var examResult = new SpirometryExamResult
        {
            OverreadFev1FvcRatio = ratio
        };

        // Act
        var actual = new FlagTextFormatter(A.Dummy<ILogger<FlagTextFormatter>>(), config)
            .FormatFlagText(examResult);

        // Assert
        Assert.Equal(expected, actual);
    }

    public static IEnumerable<object[]> FormatFlagText_TestData()
    {
        yield return
        [
            "Ratio <<overread-ratio>> LFQ <<high-risk-lfq-max-value>>", 1.5m, 10, "Ratio 1.5 LFQ 10"
        ];

        yield return
        [
            "Ratio <<overread-ratio>> LFQ <<high-risk-lfq-max-value>>", 1m, 10, "Ratio 1 LFQ 10"
        ];

        // Null ratio
        yield return
        [
            "Ratio <<overread-ratio>> LFQ <<high-risk-lfq-max-value>>", null, 10, "Ratio  LFQ 10"
        ];

        // Nothing to replace
        yield return
        [
            "Ratio LFQ", 1.5m, 10, "Ratio LFQ"
        ];

        // Ratio referenced multiple times
        yield return
        [
            "<<overread-ratio>> <<overread-ratio>> <<overread-ratio>>", 1.5m, 10, "1.5 1.5 1.5"
        ];

        // LFQ referenced multiple times
        yield return
        [
            "<<high-risk-lfq-max-value>> <<high-risk-lfq-max-value>>", 1.5m, 10, "10 10"
        ];

        // Both referenced multiple times
        yield return
        [
            "<<overread-ratio>> <<high-risk-lfq-max-value>> | <<overread-ratio>> <<high-risk-lfq-max-value>>", 1.5m, 10, "1.5 10 | 1.5 10"
        ];

        // Ensure new lines and tabs are not escaped
        yield return
        [
            "\\n\\t\\r", default(decimal?), default(int), "\n\t\r"
        ];
    }
}