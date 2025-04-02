using Signify.Spirometry.Core.Exceptions;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Exceptions;

public class AnswerValueFormatExceptionTests
{
    [Theory]
    [InlineData(1, 1, null)]
    [InlineData(1, 2, "")]
    [InlineData(1, 2, "answer")]
    public void Constructor_SetsProperties_Tests(int questionId, int answerId, string answerValue)
    {
        var ex = new AnswerValueFormatException(questionId, answerId, answerValue);

        Assert.Equal(questionId, ex.QuestionId);
        Assert.Equal(answerId, ex.AnswerId);
        Assert.Equal(answerValue, ex.AnswerValue);
    }

    [Fact]
    public void Constructor_SetsMessage_Test()
    {
        var ex = new AnswerValueFormatException(1, 2, "answer");

        Assert.Equal("Invalid answer value format for QuestionId:1, AnswerId:2, AnswerValue:answer", ex.Message);
    }
}