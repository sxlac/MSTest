using FakeItEasy;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.Spirometry.Core.EventHandlers.Akka;
using SpiroEvents;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Signify.Spirometry.Core.Tests.EventHandlers.Akka;

public class OverreadProcessedByVendorHandlerTests
{
    [Fact]
    public async Task Handle_HappyPath()
    {
        // Arrange
        var session = new TestableMessageSession();

        var @event = new OverreadProcessed();

        var subject = new OverreadProcessedByVendorHandler(A.Dummy<ILogger<OverreadProcessedByVendorHandler>>(), session);

        // Act
        await subject.Handle(@event, default);

        // Assert
        Assert.Single(session.SentMessages);
        Assert.Equal(@event, session.SentMessages.First().Message);
    }
}