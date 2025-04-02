using Refit;
using Signify.Spirometry.Core.Exceptions;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Exceptions;

public class ReleaseHoldRequestExceptionTests
{
    [Theory]
    [MemberData(nameof(Constructor_SetsProperties_TestData))]
    public void Constructor_SetsProperties_Tests(long evaluationId, int holdId, Guid cdiHoldId, ApiException exception)
    {
        var ex = new ReleaseHoldRequestException(evaluationId, holdId, cdiHoldId, exception);

        Assert.Equal(evaluationId, ex.EvaluationId);
        Assert.Equal(holdId, ex.HoldId);
        Assert.Equal(cdiHoldId, ex.CdiHoldId);
        Assert.Equal(exception.StatusCode, ex.StatusCode);
        Assert.Equal(exception.Content, ex.ResponseContent);
        Assert.Equal(exception.ReasonPhrase, ex.ReasonPhrase);
    }

    public static IEnumerable<object[]> Constructor_SetsProperties_TestData()
    {
        yield return
        [
            1,
            2,
            Guid.NewGuid(),
            new FakeApiException(HttpMethod.Put, HttpStatusCode.NotFound, "content")
        ];
    }

    [Fact]
    public void Constructor_SetsMessage_Test()
    {
        var ex = new ReleaseHoldRequestException(1, 2, Guid.Empty,
            new FakeApiException(HttpMethod.Put, HttpStatusCode.NotFound));

        Assert.Equal("Failed to release hold with CdiHoldId=00000000-0000-0000-0000-000000000000 for EvaluationId=1, with StatusCode=NotFound", ex.Message);
    }
}