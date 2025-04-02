using Signify.Spirometry.Core.Exceptions;
using System;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Exceptions;

public class UnableToDeterminePayableExceptionTests
{
    [Fact]
    public void Constructor_SetsProperties_Tests()
    {
        Guid eventId = Guid.Empty;
        long evaluationId = 1;
        var ex = new UnableToDeterminePayableException(eventId, evaluationId);

        Assert.Equal(eventId, ex.EventId);
        Assert.Equal(evaluationId, ex.EvaluationId);
    }

    [Fact]
    public void Constructor_SetsMessage_Test()
    {
        var eventId = Guid.Empty;
        const long evaluationId = 1;
        var ex = new UnableToDeterminePayableException(eventId, evaluationId);

        Assert.Equal($"Insufficient information known about evaluation to determine payable, for EventId={eventId}, EvaluationId={evaluationId}", ex.Message);
    }
}