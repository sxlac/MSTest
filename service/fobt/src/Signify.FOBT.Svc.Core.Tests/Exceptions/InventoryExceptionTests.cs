using Signify.FOBT.Svc.Core.Exceptions;
using System.Collections.Generic;
using System;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Exceptions;

public class InventoryExceptionTests
{
    [Theory]
    [MemberData(nameof(Constructor_SetsProperties_TestData))]
    public void Constructor_SetsProperties_Tests(long evaluationId, int fobtId, string messageId)
    {
        var ex = new InventoryException(evaluationId, fobtId, messageId, default);

        Assert.Equal(fobtId, ex.FobtId);
        Assert.Equal(evaluationId, ex.EvaluationId);
        Assert.Equal(messageId, ex.MessageId);
    }

    public static IEnumerable<object[]> Constructor_SetsProperties_TestData()
    {
        yield return
        [
            1,
            2,
            Guid.NewGuid().ToString()
        ];

        yield return
        [
            long.MaxValue,
            int.MaxValue,
            Guid.NewGuid().ToString()
        ];
    }
}