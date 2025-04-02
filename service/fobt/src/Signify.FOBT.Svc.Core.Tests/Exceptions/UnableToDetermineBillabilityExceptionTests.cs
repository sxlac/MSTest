using Signify.FOBT.Svc.Core.Exceptions;
using System.Collections.Generic;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Exceptions;

public class UnableToDetermineBillabilityExceptionTests
{
    [Theory]
    [MemberData(nameof(Constructor_SetsProperties_TestData))]
    public void Constructor_SetsProperties_Tests(long evaluationId)
    {
        var ex = new UnableToDetermineBillabilityException(evaluationId);

        Assert.Equal(evaluationId, ex.EvaluationId);
    }

    public static IEnumerable<object[]> Constructor_SetsProperties_TestData()
    {
        yield return
        [
            1
        ];

        yield return
        [
            long.MaxValue
        ];
    }

    [Fact]
    public void Constructor_SetsMessage_Test()
    {
        var ex = new UnableToDetermineBillabilityException(1);

        Assert.Equal("Insufficient information known about evaluation to determine billability, for EvaluationId=1", ex.Message);
    }
}