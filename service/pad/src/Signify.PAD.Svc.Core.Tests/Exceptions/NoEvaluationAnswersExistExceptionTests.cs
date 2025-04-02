using Signify.PAD.Svc.Core.Exceptions;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Exceptions;

public class NoEvaluationAnswersExistExceptionTests
{
    [Fact]
    public void Constructor_SetsProperties_Test()
    {
        // Arrange
        const long evaluationId = 1;

        // Act
        var ex = new NoEvaluationAnswersExistException(evaluationId);

        // Assert
        Assert.Equal(evaluationId, ex.EvaluationId);
    }

    [Fact]
    public void Constructor_SetsMessage_Test()
    {
        const long evaluationId = 1;

        // Act
        var ex = new NoEvaluationAnswersExistException(evaluationId);

        // Assert
        Assert.Equal("Evaluation API didn't return any answers for EvaluationId 1", ex.Message);
    }
}