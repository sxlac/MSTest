using FakeItEasy;
using Signify.Spirometry.Core.ApiClients.CdiApi.Flags.Requests;
using Signify.Spirometry.Core.ApiClients.CdiApi.Flags.Responses;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Maps;
using Signify.Spirometry.Core.Services.Flags;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using Xunit;
using NormalityIndicator = Signify.Spirometry.Core.Models.NormalityIndicator;

namespace Signify.Spirometry.Core.Tests.Maps;

public class SaveSystemFlagRequestMapperTests
{
    private const string FlagText = "text";

    private readonly IFlagTextFormatter _formatter = A.Fake<IFlagTextFormatter>();

    public SaveSystemFlagRequestMapperTests()
    {
        A.CallTo(() => _formatter.FormatFlagText(A<SpirometryExamResult>._))
            .Returns(FlagText);
    }

    private SaveSystemFlagRequestMapper CreateSubject() => new(_formatter);

    [Fact]
    public void Convert_FromSpirometryExam_ReturnsSameInstanceAsUpdated()
    {
        var destination = new SaveSystemFlagRequest();

        var actual = CreateSubject().Convert(new SpirometryExam(), destination, default);

        Assert.Equal(destination, actual);
    }

    [Fact]
    public void Convert_FromSpirometryExamResult_ReturnsSameInstanceAsUpdated()
    {
        var destination = new SaveSystemFlagRequest();

        var actual = CreateSubject().Convert(new SpirometryExamResult(), destination, default);

        Assert.Equal(destination, actual);
    }

    [Fact]
    public void Convert_FromSpirometryExam_WithNullDestination_ReturnsNotNull()
    {
        var actual = CreateSubject().Convert(new SpirometryExam(), null, default);

        Assert.NotNull(actual);
    }

    [Fact]
    public void Convert_FromSpirometryExamResult_WithNullDestination_ReturnsNotNull()
    {
        var actual = CreateSubject().Convert(new SpirometryExamResult(), null, default);

        Assert.NotNull(actual);
    }

    [Fact]
    public void Convert_FromSpirometryExam_WithNullDestinationFlag_InstantiatesFlag()
    {
        var destination = new SaveSystemFlagRequest
        {
            SystemFlag = null
        };

        CreateSubject().Convert(new SpirometryExam(), destination, default);

        Assert.NotNull(destination.SystemFlag);
    }

    [Fact]
    public void Convert_FromSpirometryExamResult_WithNullDestinationFlag_InstantiatesFlag()
    {
        var destination = new SaveSystemFlagRequest
        {
            SystemFlag = null
        };

        CreateSubject().Convert(new SpirometryExamResult(), destination, default);

        Assert.NotNull(destination.SystemFlag);
    }

    [Fact]
    public void Convert_FromSpirometryExam_Test()
    {
        // Arrange
        const int evaluationId = 1;

        var source = new SpirometryExam
        {
            EvaluationId = evaluationId
        };

        var destination = new SaveSystemFlagRequest
        {
            SystemFlag = new CdiSystemFlag()
        };

        // Act
        var actual = CreateSubject().Convert(source, destination, default);

        // Assert
        Assert.Equal(evaluationId, actual.EvaluationId);

        Assert.Equal("Signify.Spirometry.Svc", actual.ApplicationId);

        Assert.Equal(100298, actual.SystemFlag.QuestionId);
        Assert.Equal(51002, actual.SystemFlag.AnswerId);
    }

    [Fact]
    public void Convert_FromSpirometryExamResult_Notes_Test()
    {
        // Arrange
        const int spirometryExamResultsId = 1;

        var source = new SpirometryExamResult
        {
            SpirometryExamResultsId = spirometryExamResultsId
        };

        var destination = new SaveSystemFlagRequest
        {
            SystemFlag = new CdiSystemFlag()
        };

        // Act
        var actual = CreateSubject().Convert(source, destination, default);

        // Assert
        A.CallTo(() => _formatter.FormatFlagText(A<SpirometryExamResult>.That.Matches(s =>
                s.SpirometryExamResultsId == spirometryExamResultsId)))
            .MustHaveHappened();

        Assert.Equal(FlagText, actual.SystemFlag.Notes);
    }

    [Theory]
    [MemberData(nameof(Convert_FromSpirometryExamResult_AdminNotes_TestData))]
    public void Convert_FromSpirometryExamResult_AdminNotes_Tests(NormalityIndicator normalityIndicator)
    {
        // Arrange
        const decimal overreadRatio = 0.63m;

        // Unfortunately, when I created this enum, I didn't explicitly define the values to be one-based
        // I'll probably change this later; tech debt...
        var enumOffset = Enum.GetValues<NormalityIndicator>().Min(each => (short)each) == 0
            ? 1 : 0;

        var source = new SpirometryExamResult
        {
            OverreadFev1FvcRatio = overreadRatio,
            NormalityIndicatorId = (short)((short) normalityIndicator + enumOffset)
        };

        var destination = new SaveSystemFlagRequest
        {
            SystemFlag = new CdiSystemFlag()
        };

        var sb = new StringBuilder();
        sb.Append("{\"Type\":\"Spirometry\",\"Data\":{\"OverreadFev1FvcRatio\":0.63,\"ObstructionPerOverread\":");
        switch (normalityIndicator)
        {
            case NormalityIndicator.Undetermined:
                sb.Append("null");
                break;
            case NormalityIndicator.Normal:
                sb.Append("false"); // No obstruction
                break;
            case NormalityIndicator.Abnormal:
                sb.Append("true"); // Obstruction
                break;
            default:
                throw new NotImplementedException("Test data is not properly configured");
        }
        sb.Append("}}");

        var expected = sb.ToString();

#pragma warning disable S125 // SonarQube: Sections of code should not be commented out
        // Looks like this, but the JSON is minified:
        /*
         * {
         *   "Type": "Spirometry",
         *   "Data":
         *   {
         *     "OverreadFev1FvcRatio": 0.63,
         *     "ObstructionPerOverread": true
         *   }
         * }
         */
#pragma warning restore S125

        // Act
        var actual = CreateSubject().Convert(source, destination, default);

        // Assert
        Assert.Equal(expected, actual.SystemFlag.AdminNotes);
    }

    public static IEnumerable<object[]> Convert_FromSpirometryExamResult_AdminNotes_TestData()
    {
        return Enum.GetValues<NormalityIndicator>()
            .Select(normality => new object[] {normality});
    }
}