using FakeItEasy;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.Spirometry.Core.EventHandlers.Akka;
using Signify.Spirometry.Core.Events;
using Signify.Spirometry.Core.Filters;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.Spirometry.Core.Tests.EventHandlers.Akka;

public class CdiPassedEventHandlerTest
{
    private readonly TestableEndpointInstance _endpointInstance;
    private readonly CdiPassedEventHandler _cdiPassedHandler;
    private readonly IProductFilter _productFilter;
    private readonly FakeApplicationTime _applicationTime;

    public CdiPassedEventHandlerTest()
    {
        _endpointInstance = new TestableEndpointInstance();
        _productFilter = A.Fake<IProductFilter>();
        _applicationTime = new FakeApplicationTime();
        _cdiPassedHandler = new CdiPassedEventHandler(A.Dummy<ILogger<CdiPassedEventHandler>>(), _endpointInstance, _productFilter,
            _applicationTime);
    }

    [Fact]
    public async Task Handle_WithoutSpiroProduct_IsIgnored()
    {
        var @event = A.Fake<CDIPassedEvent>();
        A.CallTo(() => _productFilter.ShouldProcess(A<IEnumerable<DpsProduct>>._)).Returns(false);

        await _cdiPassedHandler.Handle(@event, CancellationToken.None);

        Assert.Empty(_endpointInstance.SentMessages);
    }

    [Fact]
    public async Task Handle_WithSpirometryProduct_IsProcessed()
    {
        var @event = A.Fake<CDIPassedEvent>();
        A.CallTo(() => _productFilter.ShouldProcess(A<List<DpsProduct>>._)).Returns(true);

        await _cdiPassedHandler.Handle(@event, CancellationToken.None);

        Assert.Single(_endpointInstance.SentMessages);
        var message = _endpointInstance.FindSentMessage<CDIPassedEvent>();
        Assert.NotNull(message);
        message.ReceivedBySpiroDateTime = _applicationTime.UtcNow();
    }
}