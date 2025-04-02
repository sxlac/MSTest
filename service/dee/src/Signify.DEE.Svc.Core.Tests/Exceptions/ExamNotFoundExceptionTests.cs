using Signify.DEE.Svc.Core.Exceptions;
using System;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Exceptions;

public class ExamNotFoundExceptionTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        const long evaluationId = 1;
        var eventId = Guid.NewGuid();

        var ex = new ExamNotFoundException(evaluationId, eventId);

        Assert.Equal(evaluationId, ex.EvaluationId);
        Assert.Equal(eventId, ex.EventId);
    }

    [Fact]
    public void Constructor_SetsMessage()
    {
        const long evaluationId = 1;
        var eventId = Guid.Parse("5a5ae438-fd89-43b7-8736-3c7e0dee86d8");

        var ex = new ExamNotFoundException(evaluationId, eventId);

        Assert.Equal(evaluationId, ex.EvaluationId);
        Assert.Equal(eventId, ex.EventId);
        Assert.Equal("Unable to find an exam with EvaluationId=1, for EventId=5a5ae438-fd89-43b7-8736-3c7e0dee86d8", ex.Message);
    }
}