using System;
using Signify.DEE.Svc.Core.Exceptions;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Exceptions;

public class ProviderPayExceptionTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        const string evaluationId = "1";
        const string message = "Error";
        var eventId = Guid.NewGuid();

        var ex = new ProviderPayException(evaluationId, eventId, message);

        Assert.Equal(eventId, ex.EventId);
        Assert.Equal(evaluationId, ex.EvaluationId);
    }

    [Fact]
    public void Constructor_SetsMessage()
    {
        const string evaluationId = "1";
        const string message = "Error";
        var eventId = Guid.NewGuid();

        var ex = new ProviderPayException(evaluationId, eventId, message);

        Assert.Equal(eventId, ex.EventId);
        Assert.Equal(evaluationId, ex.EvaluationId);
        Assert.Equal($"{message}; EvaluationId={evaluationId}, EventId={eventId}", ex.Message);
    }
}