using Signify.HBA1CPOC.Svc.Core.Exceptions;
using System;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.Exceptions;

public class ExamWithStatusNotFoundExceptionTests
{
    [Fact]
    public void Constructor_SetsProperties_Test()
    {
        const long evaluationId = 1;
        var eventId = Guid.NewGuid();
        const string status = "Performed";

        var ex = new ExamWithStatusNotFoundException(evaluationId, eventId, status);

        Assert.Equal(evaluationId, ex.EvaluationId);
        Assert.Equal(eventId, ex.EventId);
    }

    [Fact]
    public void Constructor_SetsMessage_Test()
    {
        const long evaluationId = 1;
        var eventId = Guid.Parse("5a5ae438-fd89-43b7-8736-3c7e0dee86d8");
        const string status = "Performed";

        var ex = new ExamWithStatusNotFoundException(evaluationId, eventId, status);

        Assert.Equal("Exam with EvaluationId=1 and Status=Performed not found in DB. EventId=5a5ae438-fd89-43b7-8736-3c7e0dee86d8.", ex.Message);
    }
}