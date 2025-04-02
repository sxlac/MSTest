using System;
using Signify.DEE.Svc.Core.Exceptions;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Exceptions;

public class GradeLateralityExceptionTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        const long evaluationId = 1;
        const string message = "Error";

        var ex = new GradeLateralityException(evaluationId, message);

        Assert.Equal(evaluationId, ex.EvaluationId);
    }

    [Fact]
    public void Constructor_SetsCustomMessage()
    {
        const long evaluationId = 1;
        const string message = "EvaluationAPI";

        var ex = new GradeLateralityException(evaluationId, message);

        Assert.Equal(evaluationId, ex.EvaluationId);
        Assert.Equal($"{message}", ex.Message);
    }

    [Fact]
    public void Constructor_SetsMessage_Test()
    {
        const long evaluationId = 1;

        var ex = new GradeLateralityException(evaluationId);

        Assert.Equal(evaluationId, ex.EvaluationId);
        Assert.Equal($"Invalid Laterality Code received while submitting Laterality Grade, for EvaluationId={evaluationId}", ex.Message);
    }
}