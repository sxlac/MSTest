using Signify.Spirometry.Core.Configs.Loopback;
using System.Collections.Generic;
using System.Linq;
using System;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Configs.Loopback;

public class LoopbackConfigManagerTests
{
    private readonly FakeApplicationTime _applicationTime = new();

    private LoopbackConfigManager CreateSubject(LoopbackConfig config)
        => new(config, _applicationTime);

    private static IEnumerable<DiagnosisConfig> GetDummyDiagnoses()
        => new[] { new DiagnosisConfig { Name = "COPD", AnswerValue = "some value" } };

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public void BooleanConfigs_Tests(bool shouldProcessOverreads, bool shouldCreateFlags, bool shouldReleaseHolds)
    {
        var config = new LoopbackConfig
        {
            ShouldProcessOverreads = shouldProcessOverreads,
            ShouldCreateFlags = shouldCreateFlags,
            ShouldReleaseHolds = shouldReleaseHolds,
            Diagnoses = GetDummyDiagnoses()
        };

        var subject = CreateSubject(config);

        Assert.Equal(shouldProcessOverreads, subject.ShouldProcessOverreads);
        Assert.Equal(shouldCreateFlags, subject.ShouldCreateFlags);
        Assert.Equal(shouldReleaseHolds, subject.ShouldReleaseHolds);
    }

    [Fact]
    public void Diagnoses_MustContainCopd_Test()
    {
        // Arrange
        var config = new LoopbackConfig
        {
            Diagnoses = new []
            {
                new DiagnosisConfig
                {
                    Name = "Not copd",
                    AnswerValue = "some answer value"
                },
                new DiagnosisConfig
                {
                    Name = "COPD",
                    AnswerValue = string.Empty // Invalid still
                }
            }
        };

        // Act
        // Assert
        Assert.ThrowsAny<InvalidOperationException>(() => CreateSubject(config));
    }

    [Fact]
    public void GetDiagnosisConfigs_ReturnsDiagnoses()
    {
        // Arrange
        var config = new LoopbackConfig
        {
            Diagnoses = new[]
            {
                new DiagnosisConfig
                {
                    Name = "COPD",
                    AnswerValue = "answer 1"
                },
                // In case there are multiple answer values that correspond to COPD
                new DiagnosisConfig
                {
                    Name = "COPD",
                    AnswerValue = "answer 2"
                },
                new DiagnosisConfig
                {
                    Name = "Some other diagnosis",
                    AnswerValue = "answer 3"
                }
            }
        };

        // Act
        var subject = CreateSubject(config);

        var actual = subject.GetDiagnosisConfigs().ToList();

        // Assert
        Assert.Equal(config.Diagnoses.Count(), actual.Count);

        foreach (var expectedDiagnosis in config.Diagnoses)
        {
            Assert.Single(actual, diagnosisConfig =>
                diagnosisConfig.Name == expectedDiagnosis.Name &&
                diagnosisConfig.AnswerValue == expectedDiagnosis.AnswerValue);
        }
    }

    [Fact]
    public void FlagTextFormat_Test()
    {
        // Arrange
        const string format = "format";

        var config = new LoopbackConfig
        {
            FlagTextFormat = format,
            Diagnoses = GetDummyDiagnoses()
        };

        // Act
        var subject = CreateSubject(config);

        var actual = subject.FlagTextFormat;

        // Assert
        Assert.Equal(format, actual);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    public void IsVersionEnabled_Tests(int? formVersionId, bool expected)
    {
        // Arrange
        const int firstSupportedFormVersionId = 2;

        var config = new LoopbackConfig
        {
            Diagnoses = GetDummyDiagnoses(),
            FirstSupportedFormVersionId = firstSupportedFormVersionId
        };

        // Act
        var subject = CreateSubject(config);

        // Assert
        Assert.Equal(expected, subject.IsVersionEnabled(formVersionId));
    }

    [Theory]
    [MemberData(nameof(OverreadEvaluationLookupRetryDelay_TestData))]
    public void OverreadEvaluationLookupRetryDelay_Tests(LoopbackConfig config, TimeSpan expected)
    {
        // Arrange
        config.Diagnoses = GetDummyDiagnoses();

        // Act
        var subject = CreateSubject(config);

        // Assert
        Assert.Equal(expected, subject.OverreadEvaluationLookupRetryDelay);
    }

    public static IEnumerable<object[]> OverreadEvaluationLookupRetryDelay_TestData()
    {
        yield return
        [
            new LoopbackConfig(),
            TimeSpan.Zero
        ];

        yield return
        [
            new LoopbackConfig
            {
                OverreadEvaluationLookup = new LoopbackConfig.OverreadEvaluationLookupConfig
                {
                    DelayedRetrySeconds = 1
                }
            },
            new TimeSpan(0, 0, 1)
        ];
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void CanRetryOverreadEvaluationLookup_WithoutPositiveRetryDelay_ReturnsFalse(int delayedRetrySeconds)
    {
        // Arrange
        const int retryLimitSeconds = 10;

        var config = new LoopbackConfig
        {
            Diagnoses = GetDummyDiagnoses(),
            OverreadEvaluationLookup = new LoopbackConfig.OverreadEvaluationLookupConfig
            {
                DelayedRetrySeconds = delayedRetrySeconds,
                RetryLimitSeconds = retryLimitSeconds
            }
        };

        var overreadReceivedDateTime = _applicationTime.UtcNow().AddSeconds(-retryLimitSeconds * 2); // Some time before the cutoff

        // Act
        var subject = CreateSubject(config);

        var actual = subject.CanRetryOverreadEvaluationLookup(overreadReceivedDateTime);

        // Assert
        Assert.False(actual);
    }

    [Theory]
    [InlineData(10, 20, true)]
    [InlineData(20, 10, false)]
    public void CanRetryOverreadEvaluationLookup_Tests(
        int secondsBeforeNowThatOverreadWasReceived,
        int retryLimitSeconds,
        bool expectedResult)
    {
        // Arrange
        var config = new LoopbackConfig
        {
            Diagnoses = GetDummyDiagnoses(),
            OverreadEvaluationLookup = new LoopbackConfig.OverreadEvaluationLookupConfig
            {
                DelayedRetrySeconds = 1, // Any positive number
                RetryLimitSeconds = retryLimitSeconds
            }
        };

        var overreadReceivedDateTime = _applicationTime.UtcNow().AddSeconds(-secondsBeforeNowThatOverreadWasReceived);

        // Act
        var subject = CreateSubject(config);

        var actual = subject.CanRetryOverreadEvaluationLookup(overreadReceivedDateTime);

        // Assert
        Assert.Equal(expectedResult, actual);
    }
}