using Signify.DEE.Svc.Core.Exceptions;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Exceptions;

public class InvalidPdfExceptionTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        const long evaluationId = 1;
        const string message = "Error";

        var ex = new InvalidPdfException(evaluationId, message);

        Assert.Equal(evaluationId, ex.EvaluationId);
    }

    [Fact]
    public void Constructor_SetsCustomMessage()
    {
        const long evaluationId = 1;
        const string message = "Error";

        var ex = new InvalidPdfException(evaluationId, message);

        Assert.Equal(evaluationId, ex.EvaluationId);
        Assert.Equal($"{message}", ex.Message);
    }

    [Fact]
    public void Constructor_SetsMessage_Test()
    {
        const long evaluationId = 1;

        var ex = new InvalidPdfException(evaluationId);

        Assert.Equal(evaluationId, ex.EvaluationId);
        Assert.Equal($"Invalid PDF base64 string received, for EvaluationId={evaluationId}, unable to convert it to a valid PDF", ex.Message);
    }
}