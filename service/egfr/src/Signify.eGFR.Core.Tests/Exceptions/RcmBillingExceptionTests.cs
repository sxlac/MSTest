using System;
using Signify.eGFR.Core.Exceptions;
using Xunit;

namespace Signify.eGFR.Core.Tests.Exceptions;

public class RcmBillingExceptionTests
{
    [Fact]
    public void Constructor_SetsProperties_Test()
    {
        var eventId = Guid.Empty;
        const long evaluationId = 1;
        const string message = "Testing";

        var ex = new RcmBillIdException(eventId, evaluationId, message);

        Assert.Equal(eventId, ex.EventId);
        Assert.Equal(evaluationId, ex.EvaluationId);
        Assert.Equal($"{message} for EventId={eventId}, EvaluationId={evaluationId}", ex.Message);
    }
}