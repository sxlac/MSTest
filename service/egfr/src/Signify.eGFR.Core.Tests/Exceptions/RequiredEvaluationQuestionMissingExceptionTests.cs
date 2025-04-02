using Signify.eGFR.Core.Exceptions;
using Xunit;

namespace Signify.eGFR.Core.Tests.Exceptions;

public class RequiredEvaluationQuestionMissingExceptionTests
{
    [Theory]
    [InlineData(1, 900,1)]
    [InlineData(1, 900,int.MaxValue)]
    public void Constructor_SetsProperties_Tests(long evaluationId , int formVersionId, int questionId)
    {
        var ex = new RequiredEvaluationQuestionMissingException(evaluationId, formVersionId, questionId);

        Assert.Equal(evaluationId, ex.EvaluationId);
        Assert.Equal(formVersionId, ex.FormVersionId);
        Assert.Equal(questionId, ex.QuestionId);
    }

    [Fact]
    public void Constructor_SetsMessage_Test()
    {
        var ex = new RequiredEvaluationQuestionMissingException(1, 900,1);

        Assert.Equal("EvaluationID:1 with FormVersionId:900 and QuestionId:1 is required but was missing", ex.Message);
    }
}