using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.Spirometry.Core.Configs.Loopback;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Models;
using Signify.Spirometry.Core.Services;
using System.Collections.Generic;
using System;
using Xunit;
using NormalityIndicator = Signify.Spirometry.Core.Data.Entities.NormalityIndicator;
using SessionGrade = Signify.Spirometry.Core.Models.SessionGrade;

namespace Signify.Spirometry.Core.Tests.Services;

public class ExamQualityServiceTests
{
    private const int HighRiskLfqScoreMaxValue = 18;

    private readonly ILogger<ExamQualityService> _logger = A.Fake<ILogger<ExamQualityService>>();

    private ExamQualityService CreateSubject()
    {
        var config = A.Fake<IGetLoopbackConfig>();

        A.CallTo(() => config.HighRiskLfqScoreMaxValue)
            .Returns(HighRiskLfqScoreMaxValue);

        return new ExamQualityService(_logger, config);
    }

    [Theory]
    [InlineData(SessionGrade.A, true)]
    [InlineData(SessionGrade.B, true)]
    [InlineData(SessionGrade.C, true)]
    [InlineData(SessionGrade.D, false)]
    [InlineData(SessionGrade.E, false)]
    [InlineData(SessionGrade.F, false)]
    [InlineData(null, false)] // Session Grade question not answered
    public void IsSufficientQuality_Tests(SessionGrade? sessionGrade, bool expectedResult)
    {
        // Arrange
        var result = new ExamResult
        {
            SessionGrade = sessionGrade
        };

        // Act
        var actual = CreateSubject().IsSufficientQuality(result);

        // Assert
        Assert.Equal(expectedResult, actual);
    }

    [Fact]
    public void IsSufficientQuality_HandlesAllSessionGrades()
    {
        foreach (var sessionGrade in Enum.GetValues<SessionGrade>())
        {
            // Arrange
            var result = new ExamResult
            {
                SessionGrade = sessionGrade
            };

            // Act
            CreateSubject().IsSufficientQuality(result);

            // Assert
            A.CallTo(_logger)
                .MustNotHaveHappened();
        }
    }

    [Theory]
    [InlineData(SessionGrade.A, false)]
    [InlineData(SessionGrade.B, false)]
    [InlineData(SessionGrade.C, false)]
    [InlineData(SessionGrade.D, true)]
    [InlineData(SessionGrade.E, true)]
    [InlineData(SessionGrade.F, true)]
    [InlineData(null, false)] // Session Grade question not answered
    public void NeedsOverread_Tests(SessionGrade? sessionGrade, bool expectedResult)
    {
        // Arrange
        var result = new SpirometryExamResult
        {
            SessionGradeId = (short?) sessionGrade
        };

        // Act
        var actual = CreateSubject().NeedsOverread(result);

        // Assert
        Assert.Equal(expectedResult, actual);
    }

    /// <summary>
    /// Tests the test data used in <see cref="NeedsFlag_TestData"/>
    /// </summary>
    [Fact]
    public void NeedsOverread_TestTheTest()
    {
        // Arrange
        var result = new ExamQualityModelNeedingOverread();

        // Act
        var actual = CreateSubject().NeedsOverread(result.ToSpirometryExamResult());

        // Assert
        Assert.True(actual);
    }

    [Theory]
    [MemberData(nameof(NeedsFlag_TestData))]
    public void NeedsFlag_Tests(bool? expectedResult, ExamQualityModel model)
    {
        // Arrange
        var result = model.ToSpirometryExamResult();

        // Act
        var actual = CreateSubject().NeedsFlag(result);

        // Assert
        if (expectedResult != actual)
            Assert.Equal(expectedResult, actual);
    }

    /// <summary>
    /// Model used as an intermediary which can be mapped to the SpirometryExamResult entity
    /// </summary>
    public class ExamQualityModel
    {
        public bool? CopdDiagnosis { get; init; }
        public SessionGrade? SessionGrade { get; init; }
        public bool? HasHistoryOfCopd { get; init; }
        public int? LungFunctionScore { get; init; }
        public decimal? OverreadRatio { get; init; }
        public NormalityIndicator Normality { get; init; }

        public SpirometryExamResult ToSpirometryExamResult()
            => new()
            {
                CopdDiagnosis = CopdDiagnosis,
                SessionGradeId = (short?)SessionGrade,
                HasHistoryOfCopd = HasHistoryOfCopd,
                LungFunctionScore = LungFunctionScore,
                OverreadFev1FvcRatio = OverreadRatio,
                NormalityIndicatorId = Normality.NormalityIndicatorId
            };
    }

    private class ExamQualityModelNeedingOverread : ExamQualityModel
    {
        public ExamQualityModelNeedingOverread()
        {
            // Set default values which require an overread
            CopdDiagnosis = null;
            SessionGrade = Core.Models.SessionGrade.E;
            Normality = NormalityIndicator.Undetermined;
        }
    }

    private class ExamQualityModelNeedingFlag : ExamQualityModelNeedingOverread
    {
        public ExamQualityModelNeedingFlag()
        {
            // Set default values which require a flag
            HasHistoryOfCopd = true;
            LungFunctionScore = 18;
            OverreadRatio = 0.5m;
            Normality = NormalityIndicator.Abnormal;
        }
    }

    public static IEnumerable<object[]> NeedsFlag_TestData()
    {
        // Test the test
        yield return [true, new ExamQualityModelNeedingFlag()];

        yield return [false, new ExamQualityModelNeedingOverread()];

        #region Test cases from Product
        // Tests corresponding to the scenarios listed here: https://docs.google.com/spreadsheets/d/1h05_jO8pQFXvkKb-b99gw1yBZQNtYjreJFKMoBy4W60/edit#gid=0

        // Line 3: "Don't hold in DPS PM if Dx: COPD is asserted"
        yield return
        [
            false, // NeedsFlag should be false, ie "Don't Hold in the DPS PM"
            new ExamQualityModelNeedingFlag
            {
                CopdDiagnosis = true
            }
        ];

        #region Scenarios at time of POC
        // Scenario 1
        yield return
        [
            null, // Won't know until overread has been processed; Hold in the PM
            new ExamQualityModelNeedingOverread
            {
                HasHistoryOfCopd = true,
                LungFunctionScore = 18
            }
        ];

        // Scenario 2
        yield return
        [
            false,
            new ExamQualityModelNeedingOverread
            {
                HasHistoryOfCopd = false,
                LungFunctionScore = 19
            }
        ];

        // Scenario 3
        yield return
        [
            null, // Won't know until overread has been processed; Hold in the PM
            new ExamQualityModelNeedingOverread
            {
                HasHistoryOfCopd = false,
                LungFunctionScore = 18
            }
        ];

        // Scenario 4
        yield return
        [
            null, // Won't know until overread has been processed; Hold in the PM
            new ExamQualityModelNeedingOverread
            {
                HasHistoryOfCopd = true,
                LungFunctionScore = 19
            }
        ];
        #endregion Scenarios at time of POC

        #region Scenarios after overread is processed
        // Scenario 1a
        yield return
        [
            true,
            new ExamQualityModelNeedingFlag
            {
                HasHistoryOfCopd = true,
                LungFunctionScore = 18,
                Normality = NormalityIndicator.Abnormal
            }
        ];

        // Scenario 1b
        yield return
        [
            false,
            new ExamQualityModelNeedingFlag
            {
                HasHistoryOfCopd = true,
                LungFunctionScore = 18,
                Normality = NormalityIndicator.Normal
            }
        ];

        // Scenario 2, at time of POC, before an overread comes in
        yield return
        [
            false,
            new ExamQualityModelNeedingFlag
            {
                HasHistoryOfCopd = false,
                LungFunctionScore = 19,
                OverreadRatio = null,
                Normality = NormalityIndicator.Undetermined // Awaiting overread
            }
        ];

        // Scenario 2a
        yield return
        [
            false,
            new ExamQualityModelNeedingFlag
            {
                HasHistoryOfCopd = false,
                LungFunctionScore = 19,
                Normality = NormalityIndicator.Abnormal
            }
        ];

        // Scenario 2b
        yield return
        [
            false,
            new ExamQualityModelNeedingFlag
            {
                HasHistoryOfCopd = false,
                LungFunctionScore = 19,
                Normality = NormalityIndicator.Normal
            }
        ];

        // Scenario 3a
        yield return
        [
            true,
            new ExamQualityModelNeedingFlag
            {
                HasHistoryOfCopd = false,
                LungFunctionScore = 18,
                Normality = NormalityIndicator.Abnormal
            }
        ];

        // Scenario 3b
        yield return
        [
            false,
            new ExamQualityModelNeedingFlag
            {
                HasHistoryOfCopd = false,
                LungFunctionScore = 18,
                Normality = NormalityIndicator.Normal
            }
        ];

        // Scenario 4a
        yield return
        [
            true,
            new ExamQualityModelNeedingFlag
            {
                HasHistoryOfCopd = true,
                LungFunctionScore = 19,
                Normality = NormalityIndicator.Abnormal
            }
        ];

        // Scenario 4b
        yield return
        [
            false,
            new ExamQualityModelNeedingFlag
            {
                HasHistoryOfCopd = true,
                LungFunctionScore = 19,
                Normality = NormalityIndicator.Normal
            }
        ];
        #endregion Scenarios after overread is processed
        #endregion Test cases from Product

        #region Additional test cases for added coverage
        // Now let's start updating each property of the ExamQualityModel*NeedingFlag*, one at a time, and validating NeedsFlag is what we expect

        #region Session Grade overrides
        yield return
        [
            false,
            new ExamQualityModelNeedingFlag
            {
                SessionGrade = SessionGrade.A
            }
        ];
        yield return
        [
            false,
            new ExamQualityModelNeedingFlag
            {
                SessionGrade = SessionGrade.B
            }
        ];
        yield return
        [
            false,
            new ExamQualityModelNeedingFlag
            {
                SessionGrade = SessionGrade.C
            }
        ];
        yield return
        [
            true,
            new ExamQualityModelNeedingFlag
            {
                SessionGrade = SessionGrade.D
            }
        ];
        yield return
        [
            true,
            new ExamQualityModelNeedingFlag
            {
                SessionGrade = SessionGrade.E
            }
        ];
        yield return
        [
            true,
            new ExamQualityModelNeedingFlag
            {
                SessionGrade = SessionGrade.F
            }
        ];
        yield return
        [
            false,
            new ExamQualityModelNeedingFlag
            {
                SessionGrade = null // Question not answered
            }
        ];
        #endregion Session Grade Overrides

        #region Hx of COPD Overrides
        yield return
        [
            false,
            new ExamQualityModelNeedingFlag
            {
                HasHistoryOfCopd = null // Finalized before we started tracking this Q; therefore shouldn't ever require an overread for it since it's older
            }
        ];
        #endregion Hx of COPD Overrides

        #region Lung Function Questionnaire Score Overrides
        yield return
        [
            false,
            new ExamQualityModelNeedingFlag
            {
                LungFunctionScore = null // Finalized before we started tracking this Q; therefore shouldn't ever require an overread for it since it's older
            }
        ];

        // All high-risk LFQ scores; these should still require an overread
        int lfq;
        for (lfq = 1; lfq < HighRiskLfqScoreMaxValue; ++lfq)
        {
            yield return
            [
                true,
                new ExamQualityModelNeedingFlag
                {
                    LungFunctionScore = lfq
                }
            ];
        }

        // Scenario 2 additional tests - Low-risk LFQ scores (ie >18); these should not require an overread if there's also no Hx of COPD
        lfq = HighRiskLfqScoreMaxValue + 1;
        do
        {
            yield return
            [
                false,
                new ExamQualityModelNeedingFlag
                {
                    HasHistoryOfCopd = false,
                    LungFunctionScore = lfq
                }
            ];
        } while (++lfq < HighRiskLfqScoreMaxValue + 20); // Some arbitrary upper-limit to stop the iterations
        #endregion Lung Function Questionnaire Score Overrides

        // Overread has not been processed yet; not able to determine if we need a flag until we get the overread results
        yield return
        [
            null,
            new ExamQualityModelNeedingFlag
            {
                OverreadRatio = null
            }
        ];

        // Undetermined overread result should not create a flag
        yield return
        [
            false,
            new ExamQualityModelNeedingFlag
            {
                Normality = NormalityIndicator.Undetermined
            }
        ];

        // Normal overread result should not create a flag
        yield return
        [
            false,
            new ExamQualityModelNeedingFlag
            {
                Normality = NormalityIndicator.Normal
            }
        ];
        #endregion Additional test cases for added coverage
    }
}