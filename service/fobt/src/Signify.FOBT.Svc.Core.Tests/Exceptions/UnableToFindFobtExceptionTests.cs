using Signify.FOBT.Svc.Core.Exceptions;
using System.Collections.Generic;
using System;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Exceptions;

public class UnableToFindFobtExceptionTests
{
    [Theory]
    [MemberData(nameof(Constructor_OrderCorrelationId_SetsProperties_TestData))]
    public void Constructor_OrderCorrelationId_SetsProperties_Tests(Guid orderCorrelationId, string barcode)
    {
        var ex = new UnableToFindFobtException(orderCorrelationId, barcode);

        Assert.Equal(orderCorrelationId, ex.OrderCorrelationId);
        Assert.Equal(barcode, ex.Barcode);
        Assert.Equal(default, ex.EvaluationId);
    }

    public static IEnumerable<object[]> Constructor_OrderCorrelationId_SetsProperties_TestData()
    {
        yield return
        [
            Guid.NewGuid(),
            "Barcode"
        ];

        yield return
        [
            Guid.Empty,
            null
        ];
    }

    [Fact]
    public void Constructor_OrderCorrelationId_SetsMessage_Test()
    {
        var orderCorrelationId = Guid.Parse("60119c44-a8f3-4e76-a390-0455ef053c96");

        var ex = new UnableToFindFobtException(orderCorrelationId, "barcode");

        Assert.Equal("Unable to find Fobt, for OrderCorrelationId 60119c44-a8f3-4e76-a390-0455ef053c96", ex.Message);
    }

    [Fact]
    public void Constructor_EvaluationId_SetsProperties()
    {
        const int evaluationId = 1;

        var ex = new UnableToFindFobtException(evaluationId);

        Assert.Equal(Guid.Empty, ex.OrderCorrelationId);
        Assert.Equal(default, ex.Barcode);
        Assert.Equal(evaluationId, ex.EvaluationId);
        Assert.Equal("Unable to find Fobt, for EvaluationId " + evaluationId, ex.Message);
    }

    [Fact]
    public void Constructor_Barcode_SetsProperties()
    {
        const string barcode = "someBarcode";

        var ex = new UnableToFindFobtException(barcode);

        Assert.Equal(Guid.Empty, ex.OrderCorrelationId);
        Assert.Equal(barcode, ex.Barcode);
        Assert.Equal(default, ex.EvaluationId);
        Assert.Equal("Unable to find Fobt, for Barcode " + barcode, ex.Message);
    }
}