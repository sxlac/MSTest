using Signify.eGFR.Core.Exceptions;
using Xunit;

namespace Signify.eGFR.Core.Tests.Exceptions;

public class UnsupportedAnswerForQuestionExceptionTests
{
    [Theory]
    [InlineData(1, 900,1, 1, null, 1)]
    [InlineData(1, 900,1, 2, "", 1)]
    [InlineData(1, 900,1, 2, "answer", 1)]
    public void Constructor_SetsProperties_Tests(long evaluationId , int formVersionId, int questionId, int answerId, string answerValue, long providerId)
    {
        var ex = new UnsupportedAnswerForQuestionException(evaluationId, formVersionId, questionId, answerId, answerValue, providerId);

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
        var ex = new UnsupportedAnswerForQuestionException(1, 900,1, 2, "answer", 1);

        Assert.Equal("EvaluationID:1 with FormVersionId:900 and QuestionId:1 has an unsupported AnswerId:2, with AnswerValue:answer, with ProviderId:1", ex.Message);
    }
}