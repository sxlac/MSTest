using Signify.uACR.Core.Exceptions;
using System;
using Xunit;

namespace Signify.uACR.Core.Tests.Exceptions;

public class RcmBillingExceptionTests
{
    [Fact]
    public void Constructor_SetsProperties_Test()
    {
        var eventId = Guid.Empty;
        const long evaluationId = 1;
        const string message = "Testing1";

        var ex = new RcmBillIdException(eventId, evaluationId, message);

        Assert.Equal(eventId, ex.EventId);
        Assert.Equal(evaluationId, ex.EvaluationId);
    }

    [Fact]
    public void Constructor_SetsMessage_Test()
    {
        var eventId = Guid.Empty;
        const long evaluationId = 1;
        const string message = "Testing1";

        var ex = new RcmBillIdException(eventId, evaluationId, message);

        Assert.Equal("Testing1 for EventId=00000000-0000-0000-0000-000000000000, EvaluationId=1", ex.Message);
    }
}