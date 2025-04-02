using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.eGFR.Core.EventHandlers.Akka;
using Signify.eGFR.Core.Events.Akka;
using Signify.eGFR.Core.FeatureFlagging;
using Signify.eGFR.Core.Filters;
using Signify.eGFR.Core.Infrastructure;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.eGFR.Core.Tests.EventHandlers.Akka;

public class CdiPassedEventHandlerTest
{
    private readonly TestableEndpointInstance _endpointInstance;
    private readonly CdiPassedEventHandler _cdiPassedHandler;
    private readonly IFeatureFlags _featureFlags;
    private readonly IProductFilter _productFilter;
    private readonly IApplicationTime _applicationTime;

    public CdiPassedEventHandlerTest()
    {
        _featureFlags = A.Fake<IFeatureFlags>();
        _endpointInstance = new TestableEndpointInstance();
        _productFilter = A.Fake<IProductFilter>();
        _applicationTime = A.Fake<IApplicationTime>();
        _cdiPassedHandler = new CdiPassedEventHandler(A.Dummy<ILogger<CdiPassedEventHandler>>(), _endpointInstance, _featureFlags, _productFilter, _applicationTime);
    }

    [Fact]
    public async Task Handle_WithNullOrNonCkdProduct_IsIgnored()
    {
        var @event = A.Fake<CDIPassedEvent>();
        A.CallTo(() => _productFilter.ShouldProcess(A<IEnumerable<DpsProduct>>._)).Returns(false);

        await _cdiPassedHandler.Handle(@event, CancellationToken.None);

        _endpointInstance.SentMessages.Length.Should().Be(0);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public async Task Handle_WithCkdProduct_IsProcessed(bool isFeatureEnabled, int count)
    {
        var @event = A.Fake<CDIPassedEvent>();
        A.CallTo(() => _featureFlags.EnableProviderPayCdi).Returns(isFeatureEnabled);
        A.CallTo(() => _productFilter.ShouldProcess(A<List<DpsProduct>>._)).Returns(true);
        
        await _cdiPassedHandler.Handle(@event, CancellationToken.None);

        _endpointInstance.SentMessages.Length.Should().Be(count);
        if (isFeatureEnabled)
            _endpointInstance.FindSentMessage<CDIPassedEvent>().Should().NotBeNull();
        else
            _endpointInstance.FindSentMessage<CDIPassedEvent>().Should().BeNull();
    }
}