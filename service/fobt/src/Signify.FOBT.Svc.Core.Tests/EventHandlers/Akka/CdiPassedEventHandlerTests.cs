using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.FOBT.Svc.Core.EventHandlers.Akka;
using Signify.FOBT.Svc.Core.Events;
using Signify.FOBT.Svc.Core.Filters;
using Signify.FOBT.Svc.Core.Infrastructure;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.EventHandlers.Akka;

public class CdiPassedEventHandlerTest
{
    private readonly TestableEndpointInstance _endpointInstance;
    private readonly CdiPassedEventHandler _handler;
    private readonly IProductFilter _productFilter;

    public CdiPassedEventHandlerTest()
    {
        _endpointInstance = new TestableEndpointInstance();
        _productFilter = A.Fake<IProductFilter>();
        var applicationTime = A.Fake <IApplicationTime>();
        _handler = new CdiPassedEventHandler(A.Dummy<ILogger<CdiPassedEventHandler>>(), _endpointInstance, _productFilter, applicationTime);
    }

    [Fact]
    public async Task Handle_WithNullOrNonCkdProduct_IsIgnored()
    {
        var @event = A.Fake<CDIPassedEvent>();
        A.CallTo(() => _productFilter.ShouldProcess(A<IEnumerable<DpsProduct>>._)).Returns(false);

        await _handler.Handle(@event, CancellationToken.None);

        _endpointInstance.SentMessages.Length.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithCkdProduct_IsProcessed()
    {
        var @event = A.Fake<CDIPassedEvent>();
        A.CallTo(() => _productFilter.ShouldProcess(A<List<DpsProduct>>._)).Returns(true);
        
        await _handler.Handle(@event, CancellationToken.None);

        _endpointInstance.SentMessages.Length.Should().Be(1);
        _endpointInstance.FindSentMessage<CDIPassedEvent>().Should().NotBeNull();
    }
}