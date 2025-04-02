using Signify.Spirometry.Core.Exceptions;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Exceptions;

public class RequiredEvaluationQuestionMissingExceptionTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void Constructor_SetsProperties_Tests(int questionId)
    {
        var ex = new RequiredEvaluationQuestionMissingException(questionId);

        Assert.Equal(questionId, ex.QuestionId);
    }

    [Fact]
    public void Constructor_SetsMessage_Test()
    {
        var ex = new RequiredEvaluationQuestionMissingException(1);

        Assert.Equal("QuestionId:1 is required but was missing", ex.Message);
    }
}