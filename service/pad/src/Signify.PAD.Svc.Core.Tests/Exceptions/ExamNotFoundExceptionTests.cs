using Signify.PAD.Svc.Core.Exceptions;
using System;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Exceptions;

public class ExamNotFoundExceptionTests
{
    [Fact]
    public void Constructor_SetsProperties_Test()
    {
        const long evaluationId = 1;
        var eventId = Guid.NewGuid();

        var ex = new ExamNotFoundException(evaluationId, eventId);

        Assert.Equal(evaluationId, ex.EvaluationId);
        Assert.Equal(eventId, ex.EventId);
    }

    [Fact]
    public void Constructor_SetsMessage_Test()
    {
        const long evaluationId = 1;
        var eventId = Guid.Parse("5a5ae438-fd89-43b7-8736-3c7e0dee86d8");

        var ex = new ExamNotFoundException(evaluationId, eventId);

        Assert.Equal("Exam with EvaluationId=1 not found in DB. EventId=5a5ae438-fd89-43b7-8736-3c7e0dee86d8.", ex.Message);
    }
}