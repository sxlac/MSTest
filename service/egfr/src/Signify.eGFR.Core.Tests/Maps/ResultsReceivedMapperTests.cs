using System;
using System.Collections.Generic;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Events.Akka;
using Signify.eGFR.Core.Maps;
using Xunit;

namespace Signify.eGFR.Core.Tests.Maps;

public class ResultsReceivedMapperTests
{
    private readonly Exam _exam = new()
    {
        EvaluationId = 1,
        EvaluationReceivedDateTime = new DateTime(2023, 01, 02, 3, 4, 5, DateTimeKind.Utc),
        DateOfService = new DateTime(2023, 01, 02, 3, 4, 5, DateTimeKind.Utc)
    };

    [Fact]
    public void Convert_FromExam_ReturnsSameInstanceAsUpdated()
    {
        var subject = new ResultsReceivedMapper();
        var destination = new ResultsReceived();
        var actual = subject.Convert(_exam, destination, default);

        Assert.Equal(destination, actual);
    }

    [Theory]
    [MemberData(nameof(QuestLabResults), 50, "Abnormal", "A")]
    [MemberData(nameof(QuestLabResults), 70, "Normal", "N")]
    [MemberData(nameof(QuestLabResults), null, "Undetermined", "U")]
    [MemberData(nameof(QuestLabResults), -1, "Undetermined", "U")]
    [MemberData(nameof(QuestLabResults), 0, "Undetermined", "U")]
    public void Convert_FromQuestLabResult_ReturnsSameInstanceAsUpdated(QuestLabResult questLabResult)
    {
        var subject = new ResultsReceivedMapper();
        var destination = new ResultsReceived();
        var actual = subject.Convert(questLabResult, destination, default);

        Assert.Equal(destination, actual);
    }

    [Fact]
    public void Convert_FromExam_WithNullDestination_ReturnsNotNull()
    {
        var subject = new ResultsReceivedMapper();
        var actual = subject.Convert(_exam, null, default);

        Assert.NotNull(actual);
    }

    [Theory]
    [MemberData(nameof(QuestLabResults), 50, "Abnormal", "A")]
    [MemberData(nameof(QuestLabResults), 70, "Normal", "N")]
    [MemberData(nameof(QuestLabResults), null, "Undetermined", "U")]
    [MemberData(nameof(QuestLabResults), -1, "Undetermined", "U")]
    [MemberData(nameof(QuestLabResults), 0, "Undetermined", "U")]
    public void Convert_FromQuestLabResult_WithNullDestination_ReturnsNotNull(QuestLabResult questLabResult)
    {
        var subject = new ResultsReceivedMapper();
        var actual = subject.Convert(questLabResult, null, default);

        Assert.NotNull(actual);
    }

    [Fact]
    public void Convert_FromExam_Test()
    {
        var subject = new ResultsReceivedMapper();
        var actual = subject.Convert(_exam, default, default);

        Assert.Equal(_exam.EvaluationId, actual.EvaluationId);
        Assert.Equal(_exam.DateOfService, actual.PerformedDate);
    }

    [Theory]
    [MemberData(nameof(QuestLabResults), 50, "Abnormal", "A")]
    [MemberData(nameof(QuestLabResults), 70, "Normal", "N")]
    [MemberData(nameof(QuestLabResults), null, "Undetermined", "U")]
    [MemberData(nameof(QuestLabResults), -1, "Undetermined", "U")]
    [MemberData(nameof(QuestLabResults), 0, "Undetermined", "U")]
    public void Convert_FromQuestLabResult_Test(QuestLabResult questLabResult)
    {
        var subject = new ResultsReceivedMapper();
        var actual = subject.Convert(questLabResult, default, default);

        Assert.Equal(questLabResult.NormalityCode, actual.Determination);
        Assert.NotNull(actual.Result);

        var groupActual = actual.Result;
        Assert.Equal(questLabResult.eGFRResult, groupActual.Result);
        Assert.Equal(questLabResult.NormalityCode, groupActual.AbnormalIndicator);
    }
    
    public static IEnumerable<object[]> QuestLabResults(int? egfrResultValue, string normality, string normalityCode)
    {
        yield return
        [
            new QuestLabResult
            {
                eGFRResult = egfrResultValue,
                Normality = normality,
                NormalityCode = normalityCode
            }
        ];
    }
    
    [Theory]
    [MemberData(nameof(LabResults))]
    public void Convert_FromLabResult_Test(LabResult labResult, string expectedNormality, string expectedDescription)
    {
        var subject = new ResultsReceivedMapper();
        var actual = subject.Convert(labResult, default, default);

        Assert.Equal(labResult.CreatedDateTime, actual.ReceivedDate);
        Assert.Equal(expectedNormality, actual.Determination);
        Assert.NotNull(actual.Result);

        var groupActual = actual.Result;
        
        Assert.Equal(labResult.EgfrResult, groupActual.Result);
        Assert.Equal(expectedNormality, groupActual.AbnormalIndicator);
        Assert.Equal(expectedDescription, groupActual.Description);
    }
    
    public static IEnumerable<object[]> LabResults()
    {
        yield return
        [
            new LabResult
            {
                ReceivedDate = new DateTime(2023, 01, 01, 2, 3, 5, DateTimeKind.Utc),
                EgfrResult = 59.45m,
                NormalityIndicatorId = 3,
                ResultDescription = "unknown"
            },
            "A",
            "unknown"
        ];
        yield return
        [
            new LabResult
            {
                ReceivedDate = new DateTime(2023, 02, 02, 3, 4, 5, DateTimeKind.Utc),
                EgfrResult = 70.45m,
                NormalityIndicatorId = 2,
                ResultDescription = null
            },
            "N",
            null
        ];
        yield return
        [
            new LabResult
            {
                ReceivedDate = new DateTime(2023, 07, 02, 5, 4, 5, DateTimeKind.Utc),
                EgfrResult = 70.45m,
                NormalityIndicatorId = 2,
                ResultDescription = null
            },
            "N",
            null
        ];
        yield return
        [
            new LabResult
            {
                ReceivedDate = new DateTime(2023, 08, 02, 3, 9, 5, DateTimeKind.Utc),
                EgfrResult = 0.00m,
                NormalityIndicatorId = 1,
                ResultDescription = ""
            },
            "U",
            ""
        ];
        yield return
        [
            new LabResult
            {
                ReceivedDate = new DateTime(2023, 01, 02, 3, 4, 5, DateTimeKind.Utc),
                EgfrResult = -1.00m,
                NormalityIndicatorId = 1,
                ResultDescription = ""
            },
            "U",
            ""
        ];
    }
}