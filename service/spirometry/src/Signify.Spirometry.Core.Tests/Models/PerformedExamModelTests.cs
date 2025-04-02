using Signify.Spirometry.Core.Models;
using System.Collections.Generic;
using System;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Models;

public class PerformedExamModelTests
{
    private const int EvaluationId = 1;

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void Construct_WithInvalidEvaluationId_ThrowsArgOutOfRangeEx(int evaluationId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PerformedExamModel(evaluationId, new RawExamResult()));
    }

    [Fact]
    public void Construct_WithNullExamResults_ThrowsArgNullEx()
    {
        Assert.Throws<ArgumentNullException>(() => new PerformedExamModel(EvaluationId, null));
    }

    [Fact]
    public void Construct_SetsEvaluationId()
    {
        var model = new PerformedExamModel(EvaluationId, new RawExamResult());

        Assert.Equal(EvaluationId, model.EvaluationId);
    }

    [Fact]
    public void Construct_WithExamResult_SetsExamResult()
    {
        var info = new RawExamResult();

        var model = new PerformedExamModel(EvaluationId, info);

        Assert.Equal(info, model.ExamResult);
    }

    [Theory]
    [MemberData(nameof(IEquatable_TestData))]
    public void IEquatable_Tests(bool expectedEquals, PerformedExamModel lhs, PerformedExamModel rhs)
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
            false, new PerformedExamModel(1, new RawExamResult()), null
        ];

        yield return
        [
            false,
            new PerformedExamModel(1, new RawExamResult()),
            new PerformedExamModel(2, new RawExamResult())
        ];

        yield return
        [
            false,
            new PerformedExamModel(1, new RawExamResult{CopdDiagnosis = true}),
            new PerformedExamModel(1, new RawExamResult{CopdDiagnosis = false})
        ];

        yield return
        [
            true,
            new PerformedExamModel(1, new RawExamResult()),
            new PerformedExamModel(1, new RawExamResult())
        ];
    }
}