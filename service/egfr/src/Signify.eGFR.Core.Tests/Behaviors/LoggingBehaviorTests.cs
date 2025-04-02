using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.eGFR.Core.Behaviors;
using Xunit;

namespace Signify.eGFR.Core.Tests.Behaviors;

public class LoggingBehaviorTests
{
    [Fact]
    public async Task Handle_ShouldLogMessages()
    {
        // Arrange
        var logger = new TestLogger<TestRequest>();
        var request = new TestRequest();
        const string response = "TestResponse";
        RequestHandlerDelegate<string> next = () => Task.FromResult(response);
        var behavior = new LoggingBehavior<TestRequest, string>(logger);

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(response, result);
    }

    private class TestLogger<T> : ILogger<T>
    {
        public string LastLogMessage { get; private set; }

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            LastLogMessage = formatter(state, exception);
        }
    }

    private class TestRequest : IRequest<string>
    {
        public override string ToString() => "TestRequest";
    }
}