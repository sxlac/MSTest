using System;
using System.Collections.Generic;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Events.Akka;
using Signify.uACR.Core.Maps;
using Xunit;

namespace Signify.uACR.Core.Tests.Maps;

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

  
    [Fact]
    public void Convert_FromExam_WithNullDestination_ReturnsNotNull()
    {
        var subject = new ResultsReceivedMapper();
        var actual = subject.Convert(_exam, null, default);

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
    [MemberData(nameof(LabResults))]
    public void Convert_FromLabResult_Test(LabResult labResult, string expectedNormality, string expectedDescription)
    {
        var subject = new ResultsReceivedMapper();
        var actual = subject.Convert(labResult, default, default);

        Assert.Equal(labResult.CreatedDateTime, actual.ReceivedDate);
        Assert.Equal(expectedNormality, actual.Determination);
        Assert.NotNull(actual.Result);

        var groupActual = actual.Result;
        
        Assert.Equal(labResult.UacrResult, groupActual.UacrResult);
        Assert.Equal(expectedNormality, groupActual.AbnormalIndicator);
        Assert.Equal(expectedDescription, groupActual.Description);
    }
    
    public static IEnumerable<object[]> LabResults()
    {
        yield return
        [
            new LabResult
            {
                ReceivedDate = new DateTime(2023, 01, 02, 3, 4, 5, DateTimeKind.Utc),
                UacrResult = 59.45m,
                NormalityCode = "A",
                ResultDescription = "unknown"
            },
            "A",
            "unknown"
        ];
        yield return
        [
            new LabResult
            {
                ReceivedDate = new DateTime(2023, 01, 02, 3, 4, 5, DateTimeKind.Utc),
                UacrResult = 70.45m,
                NormalityCode = "N",
                ResultDescription = null
            },
            "N",
            null
        ];
        yield return
        [
            new LabResult
            {
                ReceivedDate = new DateTime(2023, 01, 02, 3, 4, 5, DateTimeKind.Utc),
                UacrResult = 70.45m,
                NormalityCode = "N",
                ResultDescription = null
            },
            "N",
            null
        ];
        yield return
        [
            new LabResult
            {
                ReceivedDate = new DateTime(2023, 01, 02, 3, 4, 5, DateTimeKind.Utc),
                UacrResult = 0.00m,
                NormalityCode = "U",
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
                UacrResult = -1.00m,
                NormalityCode = "U",
                ResultDescription = ""
            },
            "U",
            ""
        ];
    }
}