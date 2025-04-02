using Signify.Spirometry.Core.Exceptions;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Exceptions;

public class UnsupportedAnswerForQuestionExceptionTests
{
    [Theory]
    [InlineData(1, 1, null)]
    [InlineData(1, 2, "")]
    [InlineData(1, 2, "answer")]
    public void Constructor_SetsProperties_Tests(int questionId, int answerId, string answerValue)
    {
        var ex = new UnsupportedAnswerForQuestionException(questionId, answerId, answerValue);

        Assert.Equal(questionId, ex.QuestionId);
        Assert.Equal(answerId, ex.AnswerId);
        Assert.Equal(answerValue, ex.AnswerValue);
    }

    [Fact]
    public void Constructor_SetsMessage_Test()
    {
        var ex = new UnsupportedAnswerForQuestionException(1, 2, "answer");

        Assert.Equal("QuestionId:1 has an unsupported AnswerId:2, with AnswerValue:answer", ex.Message);
    }
}