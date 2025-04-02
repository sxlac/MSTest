using System;
using Signify.eGFR.Core.Exceptions;
using Xunit;

namespace Signify.eGFR.Core.Tests.Exceptions;

public class UnableToDetermineBillabilityExceptionTests
{
    [Fact]
    public void Constructor_SetsProperties_Test()
    {
        var eventId = Guid.Empty;
        const long evaluationId = 1;

        var ex = new UnableToDetermineBillabilityException(eventId, evaluationId);

        Assert.Equal(evaluationId, ex.EvaluationId);
        Assert.Equal(eventId, ex.EventId);
    }
}