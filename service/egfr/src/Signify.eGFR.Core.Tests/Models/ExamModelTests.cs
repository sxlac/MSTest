using Signify.eGFR.Core.Models;
using System;
using Xunit;

namespace Signify.eGFR.Core.Tests.Models;

public class ExamModelTests
{
    private const int EvaluationId = 1;

    [Theory]
    [InlineData(true, -1, null)]
    [InlineData(true, 0, "")]
    [InlineData(false, -1, "This is a note")]
    [InlineData(false, 0, "This is a note")]
    public void Construct_WithInvalidEvaluationId_ThrowsArgOutOfRangeEx(bool examPerformed, int evaluationId, string notes)
    {
        if (examPerformed)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ExamModel(evaluationId, new RawExamResult()));
        }
        else
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ExamModel(evaluationId, NotPerformedReason.NotInterested, notes));
        }
    }

    [Fact]
    public void Construct_ExamPerformed_WithNullExamResults_ThrowsArgNullEx()
    {
        Assert.Throws<ArgumentNullException>(() => new ExamModel(EvaluationId, null));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Construct_SetsEvaluationId(bool examPerformed)
    {
        var model = examPerformed
            ? new ExamModel(EvaluationId, new RawExamResult())
            : new ExamModel(EvaluationId, NotPerformedReason.NotInterested, null);

        Assert.Equal(EvaluationId, model.EvaluationId);
    }

    [Fact]
    public void Construct_WithPerformedResults_SetsExamPerformedTrue()
    {
        var model = new ExamModel(EvaluationId, new RawExamResult());

        Assert.True(model.ExamPerformed);
    }

    [Fact]
    public void Construct_WithNotPerformedReason_SetsExamPerformedFalse()
    {
        var model = new ExamModel(EvaluationId, NotPerformedReason.NotInterested, null);

        Assert.False(model.ExamPerformed);
    }
}