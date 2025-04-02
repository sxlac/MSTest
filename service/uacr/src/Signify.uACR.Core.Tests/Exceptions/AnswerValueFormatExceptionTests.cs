using Signify.uACR.Core.Exceptions;
using Xunit;

namespace Signify.uACR.Core.Tests.Exceptions;

public class AnswerValueFormatExceptionTests
{
    [Theory]
    [InlineData(1, 900, 1, 2, null, 1)]
    [InlineData(1, 950, 2, 3, "", 1)]
    [InlineData(1, 800, 4, 5, "answer", 1)]
    public void Constructor_SetsProperties_Tests(long evaluationId , int formVersionId, int questionId, int answerId, string answerValue, long providerId)
    {
        var ex = new AnswerValueFormatException(evaluationId, formVersionId, questionId, answerId, answerValue, providerId);

        Assert.Equal(evaluationId, ex.EvaluationId);
        Assert.Equal(formVersionId, ex.FormVersionId);
        Assert.Equal(questionId, ex.QuestionId);
        Assert.Equal(answerId, ex.AnswerId);
        Assert.Equal(answerValue, ex.AnswerValue);
        Assert.Equal(providerId, ex.ProviderId);
    }

    [Fact]
    public void Constructor_SetsMessage_Test()
    {
        var ex = new AnswerValueFormatException(1, 900, 1, 2, "answer", 1);

        Assert.Equal("Invalid answer value format for EvaluationId:1, FormVersionId:900, QuestionId:1, AnswerId:2, AnswerValue:answer, ProviderId:1", ex.Message);
    }
}