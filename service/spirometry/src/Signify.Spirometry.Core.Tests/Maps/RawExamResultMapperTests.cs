using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.Spirometry.Core.Configs.Loopback;
using Signify.Spirometry.Core.Converters;
using Signify.Spirometry.Core.Maps;
using Signify.Spirometry.Core.Models;
using Signify.Spirometry.Core.Validators;
using System.Collections.Generic;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Maps;

public class RawExamResultMapperTests
{
    private readonly IGetLoopbackConfig _loopbackConfig = A.Fake<IGetLoopbackConfig>();
    private readonly IFvcValidator _fvcValidator = A.Fake<IFvcValidator>();
    private readonly IFev1Validator _fev1Validator = A.Fake<IFev1Validator>();
    private readonly IFev1FvcRatioValidator _fev1FvcRatioValidator = A.Fake<IFev1FvcRatioValidator>();
    private readonly IOverallNormalityConverter _overallNormalityConverter = A.Fake<IOverallNormalityConverter>();
    private readonly IFvcNormalityConverter _fvcNormalityConverter = A.Fake<IFvcNormalityConverter>();
    private readonly IFev1NormalityConverter _fev1NormalityConverter = A.Fake<IFev1NormalityConverter>();

    private RawExamResultMapper CreateSubject()
        => new(A.Dummy<ILogger<RawExamResultMapper>>(),
            _loopbackConfig, _fvcValidator, _fev1Validator, _fev1FvcRatioValidator,
            _overallNormalityConverter, _fvcNormalityConverter, _fev1NormalityConverter);

    [Fact]
    public void Convert_ReturnsSameInstanceAsUpdated()
    {
        var destination = new ExamResult
        {
            Fev1 = 25
        };

        var subject = CreateSubject();

        var actual = subject.Convert(new RawExamResult(), destination, null);

        Assert.Equal(destination, actual);
    }

    [Fact]
    public void Convert_WithNullDestination_ReturnsNotNull()
    {
        var subject = CreateSubject();

        var actual = subject.Convert(new RawExamResult(), null, null);

        Assert.NotNull(actual);
    }

    [Theory]
    [MemberData(nameof(Convert_IgnoringValidatedProperties_Tests_Data))]
    public void Convert_IgnoringValidatedProperties_Tests(RawExamResult source, ExamResult expected)
    {
        var subject = CreateSubject();

        #region Ignore these
        int? fvc, fev1;
        decimal? fev1OverFvc;

        A.CallTo(() => _fvcValidator.IsValid(A<string>._, out fvc))
            .Returns(false)
            .AssignsOutAndRefParameters(default(int?));
        A.CallTo(() => _fev1Validator.IsValid(A<string>._, out fev1))
            .Returns(false)
            .AssignsOutAndRefParameters(default(int?));
        A.CallTo(() => _fev1FvcRatioValidator.IsValid(A<string>._, out fev1OverFvc))
            .Returns(false)
            .AssignsOutAndRefParameters(default(decimal?));
        #endregion Ignore these

        var actual = subject.Convert(source, null, null);

        Assert.Equal(expected, actual);

        // Should be covered by the above since we overrode IEquality, but just in case
        Assert.Equal(expected.SessionGrade, actual.SessionGrade);
        Assert.Equal(expected.HasHighSymptom, actual.HasHighSymptom);
        Assert.Equal(expected.HasEnvOrExpRisk, actual.HasEnvOrExpRisk);
        Assert.Equal(expected.HasHighComorbidity, actual.HasHighComorbidity);
        Assert.Equal(expected.CopdDiagnosis, actual.CopdDiagnosis);
    }

    public static IEnumerable<object[]> Convert_IgnoringValidatedProperties_Tests_Data()
    {
        yield return
        [
            new RawExamResult
            {
                SessionGrade = null,
                HasHighSymptom = null,
                HasEnvOrExpRisk = null,
                HasHighComorbidity = null,
                CopdDiagnosis = null
            },
            new ExamResult
            {
                SessionGrade = null,
                HasHighSymptom = null,
                HasEnvOrExpRisk = null,
                HasHighComorbidity = null,
                CopdDiagnosis = null
            }
        ];

        yield return
        [
            new RawExamResult
            {
                SessionGrade = SessionGrade.B,
                HasHighSymptom = TrileanType.Yes,
                HasEnvOrExpRisk = TrileanType.No,
                HasHighComorbidity = TrileanType.Unknown,
                CopdDiagnosis = true
            },
            new ExamResult
            {
                SessionGrade = SessionGrade.B,
                HasHighSymptom = TrileanType.Yes,
                HasEnvOrExpRisk = TrileanType.No,
                HasHighComorbidity = TrileanType.Unknown,
                CopdDiagnosis = true
            }
        ];

        yield return
        [
            new RawExamResult
            {
                HasSmokedTobacco = true,
                TotalYearsSmoking = 3,
                ProducesSputumWithCough = false,
                CoughMucusFrequency = OccurrenceFrequency.Often,
                HadWheezingPast12mo = TrileanType.Unknown,
                GetsShortnessOfBreathAtRest = TrileanType.No,
                GetsShortnessOfBreathWithMildExertion = TrileanType.Yes,
                NoisyChestFrequency = OccurrenceFrequency.Rarely,
                ShortnessOfBreathPhysicalActivityFrequency = OccurrenceFrequency.VeryOften,
                LungFunctionQuestionnaireScore = 5
            },
            new ExamResult
            {
                HasSmokedTobacco = true,
                TotalYearsSmoking = 3,
                ProducesSputumWithCough = false,
                CoughMucusFrequency = OccurrenceFrequency.Often,
                HadWheezingPast12mo = TrileanType.Unknown,
                GetsShortnessOfBreathAtRest = TrileanType.No,
                GetsShortnessOfBreathWithMildExertion = TrileanType.Yes,
                NoisyChestFrequency = OccurrenceFrequency.Rarely,
                ShortnessOfBreathPhysicalActivityFrequency = OccurrenceFrequency.VeryOften,
                LungFunctionQuestionnaireScore = 5
            }
        ];
    }

    [Theory]
    [InlineData("1", 1, true)] // Values don't actually have to be valid as we aren't testing the validator here
    [InlineData("1", 1, false)] // Values don't actually have to be valid as we aren't testing the validator here
    public void Convert_Fvc_Tests(string rawValue, int expectedFvc, bool expectedIsValid)
    {
        int? fvc, fev1;
        decimal? fev1OverFvc;

        A.CallTo(() => _fvcValidator.IsValid(A<string>._, out fvc))
            .Returns(expectedIsValid)
            .AssignsOutAndRefParameters(expectedFvc);

        A.CallTo(() => _fev1Validator.IsValid(A<string>._, out fev1))
            .Returns(false)
            .AssignsOutAndRefParameters(default(int));
        A.CallTo(() => _fev1FvcRatioValidator.IsValid(A<string>._, out fev1OverFvc))
            .Returns(false)
            .AssignsOutAndRefParameters(default(decimal));

        var subject = CreateSubject();

        var actual = subject.Convert(new RawExamResult { Fvc = rawValue }, null, null);

        Assert.Equal(expectedFvc, actual.Fvc);
    }

    [Theory]
    [InlineData("1", 1, true)] // Values don't actually have to be valid as we aren't testing the validator here
    [InlineData("1", 1, false)] // Values don't actually have to be valid as we aren't testing the validator here
    public void Convert_Fev1_Tests(string rawValue, int expectedFev1, bool expectedIsValid)
    {
        int? fvc, fev1;
        decimal? fev1OverFvc;

        A.CallTo(() => _fvcValidator.IsValid(A<string>._, out fvc))
            .Returns(false)
            .AssignsOutAndRefParameters(default(int));

        A.CallTo(() => _fev1Validator.IsValid(A<string>._, out fev1))
            .Returns(expectedIsValid)
            .AssignsOutAndRefParameters(expectedFev1);

        A.CallTo(() => _fev1FvcRatioValidator.IsValid(A<string>._, out fev1OverFvc))
            .Returns(false)
            .AssignsOutAndRefParameters(default(decimal));

        var subject = CreateSubject();

        var actual = subject.Convert(new RawExamResult { Fev1 = rawValue }, null, null);

        Assert.Equal(expectedFev1, actual.Fev1);
    }

    [Theory]
    [InlineData("1", 1, true)] // Values don't actually have to be valid as we aren't testing the validator here
    [InlineData("1", 1, false)] // Values don't actually have to be valid as we aren't testing the validator here
    public void Convert_Fev1OverFvc_Tests(string rawValue, decimal expectedFev1OverFvc, bool expectedIsValid)
    {
        int? fvc, fev1;
        decimal? fev1OverFvc;

        A.CallTo(() => _fvcValidator.IsValid(A<string>._, out fvc))
            .Returns(false)
            .AssignsOutAndRefParameters(default(int));
        A.CallTo(() => _fev1Validator.IsValid(A<string>._, out fev1))
            .Returns(false)
            .AssignsOutAndRefParameters(default(int));

        A.CallTo(() => _fev1FvcRatioValidator.IsValid(A<string>._, out fev1OverFvc))
            .Returns(expectedIsValid)
            .AssignsOutAndRefParameters(expectedFev1OverFvc);

        var subject = CreateSubject();

        var actual = subject.Convert(new RawExamResult { Fev1FvcRatio = rawValue }, null, null);

        Assert.Equal(expectedFev1OverFvc, actual.Fev1FvcRatio);
    }

    [Fact]
    public void Convert_WhenResultsDontParse_DoesntSetResult()
    {
        int? fvc, fev1;
        decimal? fev1OverFvc;

        A.CallTo(() => _fvcValidator.IsValid(A<string>._, out fvc))
            .Returns(false)
            .AssignsOutAndRefParameters(default(int?));
        A.CallTo(() => _fev1Validator.IsValid(A<string>._, out fev1))
            .Returns(false)
            .AssignsOutAndRefParameters(default(int?));

        A.CallTo(() => _fev1FvcRatioValidator.IsValid(A<string>._, out fev1OverFvc))
            .Returns(false)
            .AssignsOutAndRefParameters(default(decimal?));

        var subject = CreateSubject();

        var actual = subject.Convert(new RawExamResult(), null, null);

        Assert.Null(actual.Fvc);
        Assert.Null(actual.Fev1);
        Assert.Null(actual.Fev1FvcRatio);
    }

    [Fact]
    public void Convert_SetsNormalityIndicator()
    {
        A.CallTo(() => _overallNormalityConverter.Convert(A<ExamResult>._))
            .Returns(NormalityIndicator.Normal);

        var subject = CreateSubject();

        var actual = subject.Convert(new RawExamResult(), null, null);

        Assert.Equal(NormalityIndicator.Normal, actual.NormalityIndicator);
    }

    [Fact]
    public void Convert_SetsIndividualNormalityIndicators()
    {
        // Actual values don't matter as we aren't testing the converter here, just using different values
        // to ensure the correct (FVC vs FEV1) normality is set for each one
        int? fvc = 1;
        int? fev1 = 2;

        const NormalityIndicator fvcNormality = NormalityIndicator.Normal;
        const NormalityIndicator fev1Normality = NormalityIndicator.Abnormal;

        A.CallTo(() => _fvcValidator.IsValid(A<string>._, out fvc))
            .Returns(true)
            .AssignsOutAndRefParameters(fvc);
        A.CallTo(() => _fev1Validator.IsValid(A<string>._, out fev1))
            .Returns(true)
            .AssignsOutAndRefParameters(fev1);

        A.CallTo(() => _fvcNormalityConverter.Convert(A<int?>.That.Matches(v => v == fvc)))
            .Returns(fvcNormality);
        A.CallTo(() => _fev1NormalityConverter.Convert(A<int?>.That.Matches(v => v == fev1)))
            .Returns(fev1Normality);

        var subject = CreateSubject();

        var actual = subject.Convert(new RawExamResult{Fvc = fvc.ToString(), Fev1 = fev1.ToString()}, null, null);

        Assert.Equal(fvcNormality, actual.FvcNormalityIndicator);
        Assert.Equal(fev1Normality, actual.Fev1NormalityIndicator);
    }

    [Fact]
    public void Convert_WhenNoCopdDiagnosisConfigsExist_DoesNotSetHasHistoryOfCopd_Test()
    {
        // Arrange
        const string someOtherDiagnosisAnswerValue = "not copd";
        A.CallTo(() => _loopbackConfig.GetDiagnosisConfigs())
            .Returns(new[]
            {
                new DiagnosisConfig
                {
                    Name = "Some other diagnosis",
                    AnswerValue = someOtherDiagnosisAnswerValue
                }
            });

        var source = new RawExamResult
        {
            PreviousDiagnoses = {someOtherDiagnosisAnswerValue}
        };

        // Act
        var subject = CreateSubject();

        var actual = subject.Convert(source, default, default);

        // Assert
        Assert.Null(actual.HasHistoryOfCopd);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Convert_HasHistoryOfCopd_Tests(bool hasHistoryOfCopd)
    {
        // Arrange
        const string copd = "COPD";
        const string copdAnswerValue = "copd answer value";
        const string someOtherDiagnosisAnswerValue = "not copd";

        A.CallTo(() => _loopbackConfig.GetDiagnosisConfigs())
            .Returns(new[]
            {
                new DiagnosisConfig
                {
                    Name = copd,
                    AnswerValue = copdAnswerValue
                },
                new DiagnosisConfig
                {
                    Name = "Some other diagnosis",
                    AnswerValue = someOtherDiagnosisAnswerValue
                }
            });

        var source = new RawExamResult
        {
            PreviousDiagnoses =
            {
                hasHistoryOfCopd ? copdAnswerValue : someOtherDiagnosisAnswerValue
            }
        };

        // Act
        var subject = CreateSubject();

        var actual = subject.Convert(source, default, default);

        // Assert
        Assert.Equal(hasHistoryOfCopd, actual.HasHistoryOfCopd);
    }
}