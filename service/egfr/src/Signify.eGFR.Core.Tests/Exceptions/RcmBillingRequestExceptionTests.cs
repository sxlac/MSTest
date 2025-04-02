using System;
using System.Net;
using Signify.eGFR.Core.Exceptions;
using Xunit;

namespace Signify.eGFR.Core.Tests.Exceptions;

public class RcmBillingRequestExceptionTests
{
    [Fact]
    public void Constructor_SetsProperties_Test()
    {
        var eventId = Guid.Empty;
        const long evaluationId = 1;
        const HttpStatusCode statusCode = HttpStatusCode.Forbidden;
        const string message = "testing";

        var ex = new RcmBillingRequestException(eventId, evaluationId, statusCode, message);

        Assert.Equal(eventId, ex.EventId);
        Assert.Equal(evaluationId, ex.EvaluationId);
        Assert.Equal(statusCode, ex.StatusCode);
        Assert.Equal($"{message} for EventId={eventId}, EvaluationId={evaluationId}, with StatusCode={statusCode}", ex.Message);
    }
}