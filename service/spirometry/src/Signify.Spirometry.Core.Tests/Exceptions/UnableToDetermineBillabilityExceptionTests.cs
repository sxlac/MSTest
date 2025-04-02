using Signify.Spirometry.Core.Exceptions;
using System.Collections.Generic;
using System;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Exceptions;

public class UnableToDetermineBillabilityExceptionTests
{
    [Theory]
    [MemberData(nameof(Constructor_SetsProperties_TestData))]
    public void Constructor_SetsProperties_Tests(Guid eventId, long evaluationId)
    {
        var ex = new UnableToDetermineBillabilityException(eventId, evaluationId);

        Assert.Equal(eventId, ex.EventId);
        Assert.Equal(evaluationId, ex.EvaluationId);
    }

    public static IEnumerable<object[]> Constructor_SetsProperties_TestData()
    {
        yield return
        [
            Guid.NewGuid(),
            1
        ];

        yield return
        [
            Guid.Empty,
            long.MaxValue
        ];
    }

    [Fact]
    public void Constructor_SetsMessage_Test()
    {
        var ex = new UnableToDetermineBillabilityException(Guid.Empty, 1);

        Assert.Equal("Insufficient information known about evaluation to determine billability, for EventId=00000000-0000-0000-0000-000000000000, EvaluationId=1", ex.Message);
    }
}