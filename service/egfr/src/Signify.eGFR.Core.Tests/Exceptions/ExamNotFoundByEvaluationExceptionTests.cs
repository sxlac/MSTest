using System;
using Signify.eGFR.Core.Exceptions;
using Xunit;

namespace Signify.eGFR.Core.Tests.Exceptions;

public class ExamNotFoundByEvaluationExceptionTests
{
    [Fact]
    public void Constructor_SetsProperties_Test()
    {
        const long evaluationId = 1;
        var eventId = Guid.Empty;

        var ex = new ExamNotFoundByEvaluationException(evaluationId, eventId);

        Assert.Equal(evaluationId, ex.EvaluationId);
        Assert.Equal(eventId, ex.EventId);
    }

    [Fact]
    public void Constructor_SetsMessage_Test()
    {
        const long evaluationId = 1;
        var eventId = Guid.Empty;

        var ex = new ExamNotFoundByEvaluationException(evaluationId, eventId);

        Assert.Equal($"Unable to find an exam with EvaluationId={evaluationId}, for EventId={eventId}", ex.Message);
    }
}