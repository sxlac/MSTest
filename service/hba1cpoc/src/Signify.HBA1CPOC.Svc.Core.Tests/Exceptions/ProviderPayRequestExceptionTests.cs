using Signify.HBA1CPOC.Svc.Core.Exceptions;
using System.Collections.Generic;
using System.Net;
using System;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.Exceptions;

public class ProviderPayRequestExceptionTests
{
    [Theory]
    [MemberData(nameof(Constructor_SetsProperties_TestData))]
    public void Constructor_SetsProperties_Tests(Guid eventId, long evaluationId, HttpStatusCode statusCode)
    {
        var ex = new ProviderPayRequestException(eventId, evaluationId, statusCode, default);

        Assert.Equal(eventId, ex.EventId);
        Assert.Equal(evaluationId, ex.EvaluationId);
        Assert.Equal(statusCode, ex.StatusCode);
    }

    public static IEnumerable<object[]> Constructor_SetsProperties_TestData()
    {
        yield return
        [
            Guid.NewGuid(),
            1,
            HttpStatusCode.InternalServerError
        ];

        yield return
        [
            Guid.Empty,
            long.MaxValue,
            HttpStatusCode.BadRequest
        ];
    }

    [Fact]
    public void Constructor_SetsMessage_Test()
    {
        var ex = new RcmBillingRequestException(Guid.Empty, 1, HttpStatusCode.BadRequest, "message");

        Assert.Equal("message for EventId=00000000-0000-0000-0000-000000000000, EvaluationId=1, with StatusCode=BadRequest", ex.Message);
    }
}