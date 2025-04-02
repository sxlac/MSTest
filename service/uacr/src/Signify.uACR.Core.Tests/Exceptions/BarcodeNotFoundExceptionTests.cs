using Signify.uACR.Core.Exceptions;
using Xunit;

namespace Signify.uACR.Core.Tests.Exceptions;

public class BarcodeNotFoundExceptionTests
{
    [Theory]
    [InlineData(1, 900, 1, 2, 999393931)]
    [InlineData(1, 950, 2, 3, 999393932)]
    [InlineData(1, 800, 4, 5, 999393933)]
    public void Constructor_SetsProperties_Tests(long evaluationId , int formVersionId, int questionId, int answerId, long providerId)
    {
        var ex = new BarcodeNotFoundException(evaluationId, formVersionId, questionId, answerId, providerId);

        Assert.Equal(evaluationId, ex.EvaluationId);
        Assert.Equal(formVersionId, ex.FormVersionId);
        Assert.Equal(questionId, ex.QuestionId);
        Assert.Equal(answerId, ex.AnswerId);
        Assert.Equal(providerId, ex.ProviderId);
    }

    [Fact]
    public void Constructor_SetsMessage_Test()
    {
        var ex = new BarcodeNotFoundException(1, 900, 1, 2, 999393939);

        Assert.Equal("Invalid answer value format for EvaluationId:1, FormVersionId:900, QuestionId:1, AnswerId:2, ProviderId:999393939", ex.Message);
    }
}