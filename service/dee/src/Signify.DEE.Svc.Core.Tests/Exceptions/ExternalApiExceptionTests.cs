using System;
using Signify.DEE.Svc.Core.Exceptions;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Exceptions;

public class ExternalApiExceptionTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        const long evaluationId = 1;
        const string apiName = "EvaluationAPI";
        var eventId = Guid.NewGuid();

        var ex = new ExternalApiException(evaluationId, eventId, apiName);

        Assert.Equal(evaluationId, ex.EvaluationId);
        Assert.Equal(apiName, ex.ApiName);
        Assert.Equal(eventId, ex.EventId);
    }

    [Fact]
    public void Constructor_SetsMessage()
    {
        const long evaluationId = 1;
        const string apiName = "EvaluationAPI";
        var eventId = Guid.Parse("5a5ae438-fd89-43b7-8736-3c7e0dee86d8");

        var ex = new ExternalApiException(evaluationId, eventId, apiName);

        Assert.Equal(evaluationId, ex.EvaluationId);
        Assert.Equal(eventId, ex.EventId);
        Assert.Equal($"Exception while accessing {apiName} APIUnable to find an exam with EvaluationId={evaluationId}, for EventId={eventId}", ex.Message);
    }
}