using Signify.Spirometry.Core.Models;
using System.Collections.Generic;
using System;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Models;

public class NotPerformedExamModelTests
{
    private const int EvaluationId = 1;

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void Construct_WithInvalidEvaluationId_ThrowsArgOutOfRangeEx(int evaluationId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new NotPerformedExamModel(evaluationId, new NotPerformedInfo(NotPerformedReason.NotInterested)));
    }

    [Fact]
    public void Construct_WithNullNotPerformedInfo_ThrowsArgNullEx()
    {
        Assert.Throws<ArgumentNullException>(() => new NotPerformedExamModel(EvaluationId, null));
    }

    [Fact]
    public void Construct_SetsEvaluationId()
    {
        var model = new NotPerformedExamModel(EvaluationId, new NotPerformedInfo(NotPerformedReason.NotInterested));

        Assert.Equal(EvaluationId, model.EvaluationId);
    }

    [Fact]
    public void Construct_WithNotPerformedInfo_SetsNotPerformedInfo()
    {
        var info = new NotPerformedInfo(NotPerformedReason.EnvironmentalIssue);

        var model = new NotPerformedExamModel(EvaluationId, info);

        Assert.Equal(info, model.NotPerformedInfo);
    }

    [Theory]
    [MemberData(nameof(IEquatable_TestData))]
    public void IEquatable_Tests(bool expectedEquals, NotPerformedExamModel lhs, NotPerformedExamModel rhs)
    {
        if (lhs != null)
            Assert.Equal(expectedEquals, lhs.Equals(rhs));
        if (rhs != null)
            Assert.Equal(expectedEquals, rhs.Equals(lhs));
        Assert.Equal(expectedEquals, lhs == rhs);
        Assert.Equal(!expectedEquals, lhs != rhs);
    }

    public static IEnumerable<object[]> IEquatable_TestData()
    {
        yield return
        [
            true, null, null
        ];

        yield return
        [
            false,
            new NotPerformedExamModel(1, new NotPerformedInfo(NotPerformedReason.NotInterested)),
            null
        ];

        yield return
        [
            false,
            new NotPerformedExamModel(1, new NotPerformedInfo(NotPerformedReason.NotInterested)),
            new NotPerformedExamModel(2, new NotPerformedInfo(NotPerformedReason.NotInterested))
        ];

        yield return
        [
            false,
            new NotPerformedExamModel(1, new NotPerformedInfo(NotPerformedReason.NotInterested)),
            new NotPerformedExamModel(1, new NotPerformedInfo(NotPerformedReason.EnvironmentalIssue))
        ];

        yield return
        [
            true,
            new NotPerformedExamModel(1, new NotPerformedInfo(NotPerformedReason.NotInterested)),
            new NotPerformedExamModel(1, new NotPerformedInfo(NotPerformedReason.NotInterested))
        ];
    }
}