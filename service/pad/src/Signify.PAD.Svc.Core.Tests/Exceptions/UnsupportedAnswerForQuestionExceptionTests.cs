using Signify.PAD.Svc.Core.Exceptions;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Exceptions;

public class UnsupportedAnswerForQuestionExceptionTests
{
    [Theory]
    [InlineData(1, 2, 3, null)]
    [InlineData(1, 2, 3, "")]
    [InlineData(1, 2, 3, "answer")]
    public void Constructor_SetsProperties_Tests(long evaluationId, int questionId, int answerId, string answerValue)
    {
        var ex = new UnsupportedAnswerForQuestionException(evaluationId, questionId, answerId, answerValue);
        Assert.Equal(evaluationId, ex.EvaluationId);
        Assert.Equal(questionId, ex.QuestionId);
        Assert.Equal(answerId, ex.AnswerId);
        Assert.Equal(answerValue, ex.AnswerValue);
    }
    [Fact]
    public void Constructor_SetsMessage_Test()
    {
        var ex = new UnsupportedAnswerForQuestionException(1, 2, 3, "answer");
        Assert.Equal("QuestionId:2 has an unsupported AnswerId:3, with AnswerValue:answer, for EvaluationId:1", ex.Message);
    }
}