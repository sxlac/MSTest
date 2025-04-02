using Signify.FOBT.Svc.Core.Data.Entities;
using Signify.FOBT.Svc.Core.Events;
using Signify.FOBT.Svc.Core.Maps;
using System;
using Xunit;

using Fobt = Signify.FOBT.Svc.Core.Data.Entities.FOBT;

namespace Signify.FOBT.Svc.Core.Tests.Maps;

public class ResultsReceivedMapperTests
{
    private readonly Fobt _defaultFobt = new()
    {
        EvaluationId = 1
    };

    [Fact]
    public void Convert_FromFobt_ReturnsSameInstanceAsUpdated()
    {
        var destination = new Results();

        var subject = new ResultsReceivedMapper();

        var actual = subject.Convert(_defaultFobt, destination, default);

        Assert.Equal(actual, destination);
    }

    [Fact]
    public void Convert_FromLabResults_ReturnsSameInstanceAsUpdated()
    {
        var destination = new Results();

        var subject = new ResultsReceivedMapper();

        var actual = subject.Convert(new LabResults(), destination, default);

        Assert.Equal(actual, destination);
    }

    [Fact]
    public void Convert_FromFobt_WithNullDestination_ReturnsNotNull()
    {
        var subject = new ResultsReceivedMapper();

        var actual = subject.Convert(_defaultFobt, null, default);

        Assert.NotNull(actual);
    }

    [Fact]
    public void Convert_FromLabResults_WithNullDestination_ReturnsNotNull()
    {
        var subject = new ResultsReceivedMapper();

        var actual = subject.Convert(new LabResults(), null, default);

        Assert.NotNull(actual);
    }

    [Fact]
    public void Convert_FromFobt_Test()
    {
        const int evaluationId = 1;

        var source = new Fobt
        {
            EvaluationId = evaluationId
        };

        var subject = new ResultsReceivedMapper();

        var actual = subject.Convert(source, default, default);

        Assert.Equal(evaluationId, actual.EvaluationId);
    }

    [Fact]
    public void Convert_FromLabResults_Test()
    {
        var performedDate = new DateTime(2023, 01, 02, 3, 4, 5, DateTimeKind.Utc);
        var receivedDate = new DateTime(2023, 01, 02, 3, 4, 6, DateTimeKind.Utc);
        const string determination = "A";
        const string barcode = "barcode";
        const string labResult = "result";
        const string exception = "exception";

        var source = new LabResults
        {
            ServiceDate = performedDate,
            CreatedDateTime = receivedDate,
            AbnormalIndicator = determination,
            Barcode = barcode,
            LabResult = labResult,
            Exception = exception
        };

        var subject = new ResultsReceivedMapper();

        var actual = subject.Convert(source, default, default);

        Assert.Equal(performedDate, actual.PerformedDate);
        Assert.Equal(receivedDate, actual.ReceivedDate);
        Assert.Equal(determination, actual.Determination);
        Assert.Equal(barcode, actual.Barcode);

        Assert.NotNull(actual.Result);
        Assert.Single(actual.Result);

        var group = actual.Result[0];

        Assert.Equal(determination, group.AbnormalIndicator);
        Assert.Equal(labResult, group.Result);
        Assert.Equal(exception, group.Exception);
    }

    [Fact]
    public void Convert_FromLabResultWithCollectionDate_ConvertSuccessfully()
    {
        var dateTime = DateTime.UtcNow;
        var performedDate = dateTime.AddDays(-5);
        var receivedDate = dateTime.AddDays(-3);
        var collectionDate = dateTime.AddDays(-2);
        const string determination = "A";
        const string barcode = "barcode";
        const string labResult = "result";
        const string exception = "exception";

        var source = new LabResults
        {
            ServiceDate = performedDate,
            CreatedDateTime = receivedDate,
            AbnormalIndicator = determination,
            Barcode = barcode,
            LabResult = labResult,
            Exception = exception,
            CollectionDate = collectionDate
        };

        var subject = new ResultsReceivedMapper();

        var actual = subject.Convert(source, default, default);

        Assert.Equal(performedDate, actual.PerformedDate);
        Assert.Equal(receivedDate, actual.ReceivedDate);
        Assert.Equal(collectionDate, actual.MemberCollectionDate);
        Assert.Equal(determination, actual.Determination);
        Assert.Equal(barcode, actual.Barcode);

        Assert.NotNull(actual.Result);
        Assert.Single(actual.Result);

        var group = actual.Result[0];

        Assert.Equal(determination, group.AbnormalIndicator);
        Assert.Equal(labResult, group.Result);
        Assert.Equal(exception, group.Exception);
    }
}