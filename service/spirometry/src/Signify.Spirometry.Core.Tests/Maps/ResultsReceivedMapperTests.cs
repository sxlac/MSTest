using FakeItEasy;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Events.Akka;
using Signify.Spirometry.Core.Maps;
using Signify.Spirometry.Core.Services;
using System.Collections.Generic;
using System;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Maps;

public class ResultsReceivedMapperTests
{
    private readonly IExamQualityService _examQualityService = A.Fake<IExamQualityService>();

    private ResultsReceivedMapper CreateSubject()
        => new(_examQualityService);

    [Fact]
    public void Convert_FromSpirometryExam_ReturnsSameInstanceAsUpdated()
    {
        var destination = new ResultsReceived();

        var subject = CreateSubject();

        var actual = subject.Convert(new SpirometryExam(), destination, default);

        Assert.Equal(destination, actual);
    }

    [Fact]
    public void Convert_FromSpirometryExamResult_ReturnsSameInstanceAsUpdated()
    {
        var destination = new ResultsReceived();

        var subject = CreateSubject();

        var actual = subject.Convert(new SpirometryExamResult(), destination, default);

        Assert.Equal(destination, actual);
    }

    [Fact]
    public void Convert_FromSpirometryExam_WithNullDestination_ReturnsNotNull()
    {
        var subject = CreateSubject();

        var actual = subject.Convert(new SpirometryExam(), null, default);

        Assert.NotNull(actual);
    }

    [Fact]
    public void Convert_FromSpirometryExamResult_WithNullDestination_ReturnsNotNull()
    {
        var subject = CreateSubject();

        var actual = subject.Convert(new SpirometryExamResult(), null, default);

        Assert.NotNull(actual);
    }

    [Fact]
    public void Convert_FromSpirometryExamResult_WithNullDestinationResults_InstantiatesResults()
    {
        var destination = new ResultsReceived
        {
            Results = null
        };

        var subject = CreateSubject();

        subject.Convert(new SpirometryExamResult(), destination, default);

        Assert.NotNull(destination.Results);
    }

    [Fact]
    public void Convert_FromSpirometryExam_Tests()
    {
        const int evaluationId = 1;

        var performedDate = DateTime.UtcNow;
        var receivedDate = DateTime.UtcNow;

        var expectedPerformedDate = new DateTimeOffset(performedDate);
        var expectedReceivedDate = new DateTimeOffset(receivedDate);

        var source = new SpirometryExam
        {
            EvaluationId = evaluationId,
            EvaluationCreatedDateTime = performedDate,
            EvaluationReceivedDateTime = receivedDate,
        };

        var subject = CreateSubject();

        var actual = subject.Convert(source, default, default);

        Assert.Equal(evaluationId, actual.EvaluationId);
        Assert.Equal(expectedPerformedDate, actual.PerformedDate);
        Assert.Equal(expectedReceivedDate, actual.ReceivedDate);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    [InlineData(false)]
    public void Convert_FromExamResult_HasSmokedTobacco_Tests(bool? hasSmokedTobacco)
    {
        var source = new SpirometryExamResult
        {
            HasSmokedTobacco = hasSmokedTobacco
        };

        var subject = CreateSubject();

        var actual = subject.Convert(source, default, default);

        Assert.Equal(hasSmokedTobacco, actual.Results.HasSmokedTobacco);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    [InlineData(false)]
    public void Convert_FromSpirometryExamResult_ProducesSputumWithCough_Tests(bool? producesSputum)
    {
        var source = new SpirometryExamResult
        {
            ProducesSputumWithCough = producesSputum
        };

        var subject = CreateSubject();

        var actual = subject.Convert(source, default, default);

        Assert.Equal(producesSputum, actual.Results.ProducesSputumWithCough);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(32)]
    public void Convert_FromSpirometryExamResult_TotalYearsSmoking_Tests(int? totalYears)
    {
        var source = new SpirometryExamResult
        {
            TotalYearsSmoking = totalYears
        };

        var subject = CreateSubject();

        var actual = subject.Convert(source, default, default);

        Assert.Equal(totalYears, actual.Results.TotalYearsSmoking);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(32)]
    public void Convert_FromSpirometryExamResult_LungFunctionScore_Tests(int? score)
    {
        var source = new SpirometryExamResult
        {
            LungFunctionScore = score
        };

        var subject = CreateSubject();

        var actual = subject.Convert(source, default, default);

        Assert.Equal(score, actual.Results.LungFunctionScore);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    [InlineData(false)]
    public void Convert_FromSpirometryExamResult_Copd_Tests(bool? copdDiagnosis)
    {
        var source = new SpirometryExamResult
        {
            CopdDiagnosis = copdDiagnosis
        };

        var subject = CreateSubject();

        var actual = subject.Convert(source, default, default);

        Assert.Equal(copdDiagnosis, actual.Results.Copd);
    }

    [Fact]
    public void Convert_FromSpirometryExamResult_HandlesAllSessionGrades()
    {
        var source = new SpirometryExamResult();

        var subject = CreateSubject();

        foreach (var sessionGrade in SessionGrade.A.GetAllEnumerations())
        {
            source.SessionGradeId = sessionGrade.SessionGradeId;

            // Should not throw an exception
            subject.Convert(source, default, default);
        }

        // Address "Add at least one assertion to this test case" warning
        Assert.True(true);
    }

    [Fact]
    public void Convert_FromSpirometryExamResult_HandlesAllNormalityIndicators()
    {
        var source = new SpirometryExamResult();

        var subject = CreateSubject();

        foreach (var normality in NormalityIndicator.Undetermined.GetAllEnumerations())
        {
            source.NormalityIndicatorId = normality.NormalityIndicatorId;
            source.FvcNormalityIndicatorId = normality.NormalityIndicatorId;
            source.Fev1NormalityIndicatorId = normality.NormalityIndicatorId;

            // Should not throw an exception
            subject.Convert(source, default, default);
        }

        // Address "Add at least one assertion to this test case" warning
        Assert.True(true);
    }

    [Fact]
    public void Convert_FromSpirometryExamResult_HandlesAllOccurrenceFrequencies()
    {
        var source = new SpirometryExamResult();

        var subject = CreateSubject();

        source.CoughMucusOccurrenceFrequencyId = null;
        source.NoisyChestOccurrenceFrequencyId = null;
        source.ShortnessOfBreathPhysicalActivityOccurrenceFrequencyId = null;

        // Should not throw an exception
        subject.Convert(source, default, default);

        foreach (var frequency in OccurrenceFrequency.Never.GetAllEnumerations())
        {
            source.CoughMucusOccurrenceFrequencyId = frequency.OccurrenceFrequencyId;
            source.NoisyChestOccurrenceFrequencyId = frequency.OccurrenceFrequencyId;
            source.ShortnessOfBreathPhysicalActivityOccurrenceFrequencyId = frequency.OccurrenceFrequencyId;

            // Should not throw an exception
            subject.Convert(source, default, default);
        }

        // Address "Add at least one assertion to this test case" warning
        Assert.True(true);
    }

    [Theory]
    [MemberData(nameof(Convert_FromSpirometryExamResult_SetsCorrectOccurrenceFrequency_TestData))]
    public void Convert_FromSpirometryExamResult_SetsCorrectOccurrenceFrequency(OccurrenceFrequency frequency, string expected)
    {
        var source = new SpirometryExamResult();

        var subject = CreateSubject();

        // Setting and converting each one individually to ensure the destination property isn't mapped from a different source frequency
        if (frequency != null)
            source.CoughMucusOccurrenceFrequencyId = frequency.OccurrenceFrequencyId;
        var result = subject.Convert(source, default, default).Results;
        Assert.Equal(expected, result.CoughMucusOccurrenceFrequency);

        if (frequency != null)
            source.NoisyChestOccurrenceFrequencyId = frequency.OccurrenceFrequencyId;
        result = subject.Convert(source, default, default).Results;
        Assert.Equal(expected, result.NoisyChestOccurrenceFrequency);

        if (frequency != null)
            source.ShortnessOfBreathPhysicalActivityOccurrenceFrequencyId = frequency.OccurrenceFrequencyId;
        result = subject.Convert(source, default, default).Results;
        Assert.Equal(expected, result.ShortnessOfBreathPhysicalActivityOccurrenceFrequency);
    }

    public static IEnumerable<object[]> Convert_FromSpirometryExamResult_SetsCorrectOccurrenceFrequency_TestData()
    {
        yield return
        [
            null, null
        ];

        yield return
        [
            OccurrenceFrequency.Never, "Never"
        ];

        yield return
        [
            OccurrenceFrequency.Rarely, "Rarely"
        ];

        yield return
        [
            OccurrenceFrequency.Sometimes, "Sometimes"
        ];

        yield return
        [
            OccurrenceFrequency.Often, "Often"
        ];

        yield return
        [
            OccurrenceFrequency.VeryOften, "Very often"
        ];
    }

    [Fact]
    public void Convert_FromSpirometryExamResult_HandlesAllTrileanTypes()
    {
        var source = new SpirometryExamResult();

        var subject = CreateSubject();

        source.HadWheezingPast12moTrileanTypeId = null;
        source.GetsShortnessOfBreathAtRestTrileanTypeId = null;
        source.GetsShortnessOfBreathWithMildExertionTrileanTypeId = null;

        // Should not throw ArgOutOfRangeEx
        subject.Convert(source, default, default);

        foreach (var trileanType in TrileanType.Unknown.GetAllEnumerations())
        {
            source.HadWheezingPast12moTrileanTypeId = trileanType.TrileanTypeId;
            source.GetsShortnessOfBreathAtRestTrileanTypeId = trileanType.TrileanTypeId;
            source.GetsShortnessOfBreathWithMildExertionTrileanTypeId = trileanType.TrileanTypeId;

            // Should not throw ArgOutOfRangeEx
            subject.Convert(source, default, default);
        }

        // Address "Add at least one assertion to this test case" warning
        Assert.True(true);
    }

    [Theory]
    [MemberData(nameof(Convert_FromSpirometryExamResult_SetsCorrectTrileanType_TestData))]
    public void Convert_FromSpirometryExamResult_SetsCorrectTrileanType(TrileanType trileanType, string expected)
    {
        var source = new SpirometryExamResult();

        var subject = CreateSubject();

        // Setting and converting each one individually to ensure the destination property isn't mapped from a different source trilean type
        if (trileanType != null)
            source.HadWheezingPast12moTrileanTypeId = trileanType.TrileanTypeId;
        var result = subject.Convert(source, default, default).Results;
        Assert.Equal(expected, result.HadWheezingPast12mo);

        if (trileanType != null)
            source.GetsShortnessOfBreathAtRestTrileanTypeId = trileanType.TrileanTypeId;
        result = subject.Convert(source, default, default).Results;
        Assert.Equal(expected, result.GetsShortnessOfBreathAtRest);

        if (trileanType != null)
            source.GetsShortnessOfBreathWithMildExertionTrileanTypeId = trileanType.TrileanTypeId;
        result = subject.Convert(source, default, default).Results;
        Assert.Equal(expected, result.GetsShortnessOfBreathWithMildExertion);
    }

    public static IEnumerable<object[]> Convert_FromSpirometryExamResult_SetsCorrectTrileanType_TestData()
    {
        yield return
        [
            null, null
        ];

        yield return
        [
            TrileanType.Unknown, "U"
        ];

        yield return
        [
            TrileanType.Yes, "Y"
        ];

        yield return
        [
            TrileanType.No, "N"
        ];
    }

    [Theory]
    [MemberData(nameof(Convert_FromSpirometryExamResult_NormalityIndicatorTestData))]
    public void Convert_FromSpirometryExamResult_NormalityIndicatorTests(
        NormalityIndicator overallNormality, NormalityIndicator fvcNormality, NormalityIndicator fev1Normality,
        string expectedDetermination, string expectedFvcNormality, string expectedFev1Normality)
    {
        var source = new SpirometryExamResult
        {
            NormalityIndicatorId = overallNormality.NormalityIndicatorId,
            FvcNormalityIndicatorId = fvcNormality.NormalityIndicatorId,
            Fev1NormalityIndicatorId = fev1Normality.NormalityIndicatorId
        };

        var subject = CreateSubject();

        var actual = subject.Convert(source, default, default);

        Assert.Equal(expectedDetermination, actual.Determination);
        Assert.Equal(expectedFvcNormality, actual.Results.FvcNormality);
        Assert.Equal(expectedFev1Normality, actual.Results.Fev1Normality);
    }

    public static IEnumerable<object[]> Convert_FromSpirometryExamResult_NormalityIndicatorTestData()
    {
        // This is a sufficient number of test cases; no need to run every (3^3=27) permutation
        yield return
        [
            NormalityIndicator.Undetermined, NormalityIndicator.Abnormal, NormalityIndicator.Normal, "U", "A", "N"
        ];

        yield return
        [
            NormalityIndicator.Abnormal, NormalityIndicator.Normal, NormalityIndicator.Undetermined, "A", "N", "U"
        ];

        yield return
        [
            NormalityIndicator.Normal, NormalityIndicator.Undetermined, NormalityIndicator.Abnormal, "N", "U", "A"
        ];

        yield return
        [
            NormalityIndicator.Normal, NormalityIndicator.Normal, NormalityIndicator.Normal, "N", "N", "N"
        ];
    }

    [Theory]
    [MemberData(nameof(Convert_FromExamResult_SessionGradeTestData))]
    public void Convert_FromExamResult_SessionGradeTests(SessionGrade sessionGrade, string expectedSessionGrade)
    {
        var source = new SpirometryExamResult
        {
            SessionGradeId = sessionGrade?.SessionGradeId
        };

        var subject = CreateSubject();

        var actual = subject.Convert(source, default, default);

        Assert.Equal(expectedSessionGrade, actual.Results.SessionGrade);
    }

    public static IEnumerable<object[]> Convert_FromExamResult_SessionGradeTestData()
    {
        yield return
        [
            null, null
        ];

        yield return
        [
            SessionGrade.A, "A"
        ];

        yield return
        [
            SessionGrade.B, "B"
        ];

        yield return
        [
            SessionGrade.C, "C"
        ];

        yield return
        [
            SessionGrade.D, "D"
        ];

        yield return
        [
            SessionGrade.E, "E"
        ];

        yield return
        [
            SessionGrade.F, "F"
        ];
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(null, (short)1)] // Actual values don't matter
    [InlineData((short)1, null)]
    [InlineData((short)1, (short)2)]
    public void Convert_FromSpirometryExamResult_Fvc_Fev1_Tests(short? fvc, short? fev1)
    {
        var source = new SpirometryExamResult
        {
            Fvc = fvc,
            Fev1 = fev1
        };

        var subject = CreateSubject();

        var actual = subject.Convert(source, default, default);

        Assert.Equal(fvc, actual.Results.Fvc);
        Assert.Equal(fev1, actual.Results.Fev1);
    }

    [Theory]
    [MemberData(nameof(Convert_FromSpirometryExamResult_Fev1OverFvc_TestData))] // decimal? doesn't play well with InlineData unfortunately
    public void Convert_FromSpirometryExamResult_Fev1OverFvc_Tests(decimal? fev1OverFvc)
    {
        var source = new SpirometryExamResult
        {
            Fev1FvcRatio = fev1OverFvc
        };

        var subject = CreateSubject();

        var actual = subject.Convert(source, default, default);

        Assert.Equal(fev1OverFvc, actual.Results.Fev1OverFvc);
    }

    public static IEnumerable<object[]> Convert_FromSpirometryExamResult_Fev1OverFvc_TestData()
    {
        yield return [null];
        yield return [decimal.Zero];
        yield return [decimal.One];
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Convert_FromSpirometryExamResult_EligibleForOverread_Tests(bool needsOverread)
    {
        // Arrange
        A.CallTo(() => _examQualityService.NeedsOverread(A<SpirometryExamResult>._))
            .Returns(needsOverread);

        // Act
        var actual = CreateSubject().Convert(new SpirometryExamResult(), default, default);

        // Assert
        Assert.Equal(needsOverread, actual.Results.EligibleForOverread);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(null, true)]
    public void Convert_FromSpirometryExamResult_WasHeldForOverread_Tests(bool? needsFlag, bool expectedResult)
    {
        // Arrange
        A.CallTo(() => _examQualityService.NeedsFlag(A<SpirometryExamResult>._))
            .Returns(needsFlag);

        // Act
        var actual = CreateSubject().Convert(new SpirometryExamResult(), default, default);

        // Assert
        Assert.Equal(expectedResult, actual.Results.WasHeldForOverread);
    }
}