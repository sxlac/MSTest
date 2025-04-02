using Signify.eGFR.Core.Exceptions;
using Xunit;

namespace Signify.eGFR.Core.Tests.Exceptions;

public class BarcodeNotFoundExceptionTests
{
    [Theory]
    [InlineData(1, 900, 1, 2, 1)]
    [InlineData(1, 950, 2, 3, 1)]
    [InlineData(1, 800, 4, 5, 1)]
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
        var ex = new BarcodeNotFoundException(1, 900, 1, 2, 1);

        Assert.Equal("Invalid answer value format for EvaluationId:1, FormVersionId:900, QuestionId:1, AnswerId:2, ProviderId:1", ex.Message);
    }
}