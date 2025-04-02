using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.PAD.Svc.Core.EventHandlers.Akka;
using Signify.PAD.Svc.Core.Events;
using Signify.PAD.Svc.Core.Filters;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.EventHandlers.Akka;

public class CdiFailedEventHandlerTests
{
    private readonly TestableEndpointInstance _endpointInstance;
    private readonly CdiFailedEventHandler _handler;
    private readonly IProductFilter _productFilter;

    public CdiFailedEventHandlerTests()
    {
        _endpointInstance = new TestableEndpointInstance();
        _productFilter = A.Fake<IProductFilter>();

        _handler = new CdiFailedEventHandler(A.Dummy<ILogger<CdiFailedEventHandler>>(), _endpointInstance, _productFilter);
    }

    [Fact]
    public async Task Handle_WithNullOrNonPadProduct_IsIgnored()
    {
        var @event = A.Fake<CDIFailedEvent>();
        A.CallTo(() => _productFilter.ShouldProcess((IEnumerable<DpsProduct>)null)).Returns(false);

        await _handler.Handle(@event, CancellationToken.None);

        _endpointInstance.SentMessages.Length.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithPadProduct_IsProcessed()
    {
        // Arrange
        var @event = A.Fake<CDIFailedEvent>();
        A.CallTo(() => _productFilter.ShouldProcess(A<List<DpsProduct>>._)).Returns(true);

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        _endpointInstance.SentMessages.Length.Should().Be(1);
        _endpointInstance.FindSentMessage<CDIFailedEvent>().Should().NotBeNull();
    }
}