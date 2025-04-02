using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.uACR.Core.Behaviors;
using Xunit;

namespace Signify.uACR.Core.Tests.Behaviors;

public class LoggingBehaviorTests
{
    private interface ITestRequest;
    private interface ITestResponse;
    private class TestRequest : ITestRequest, IRequest<TestResponse>;

    private class TestResponse : ITestResponse;

    [Fact]
    public async Task Handle_LogsCorrectInformation()
    {
        // Arrange
        var logger = new TestLogger<LoggingBehavior<TestRequest, TestResponse>>();
        var behavior = new LoggingBehavior<TestRequest, TestResponse>(logger);
        var request = new TestRequest();
        var response = new TestResponse();
        Task<TestResponse> Next() => Task.FromResult(response);

        // Act
        var result = await behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        Assert.Equal(response, result);
        Assert.Contains("Handling TestRequest", logger.Logs);
        Assert.Contains("Handled TestResponse", logger.Logs);
    }

    private class TestLogger<T> : ILogger<T>
    {
        public List<string> Logs { get; } = [];

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Logs.Add(formatter(state, exception));
        }
    }
}