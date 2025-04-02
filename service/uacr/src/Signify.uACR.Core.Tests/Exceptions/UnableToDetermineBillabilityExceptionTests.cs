using System;
using Signify.uACR.Core.Exceptions;
using Xunit;

namespace Signify.uACR.Core.Tests.Exceptions;

public class UnableToDetermineBillabilityExceptionTests
{
    [Theory]
    [InlineData(1000000)]
    [InlineData(22000000)]
    public void Constructor_SetsProperties_Tests(long evaluationId)
    {
        var ex = new UnableToDetermineBillabilityException(Guid.Empty, evaluationId);
        var eventGuid = Guid.Empty;

        Assert.Equal(evaluationId, ex.EvaluationId);
        Assert.Equal(eventGuid, ex.EventId);
    }

    [Fact]
    public void Constructor_SetsMessage_Test()
    {
        var eventId = Guid.NewGuid();
        var ex = new UnableToDetermineBillabilityException(eventId, 1);

        Assert.Equal($"Insufficient information known about evaluation to determine billability, for EventId={eventId}, EvaluationId=1", ex.Message);
    }
}