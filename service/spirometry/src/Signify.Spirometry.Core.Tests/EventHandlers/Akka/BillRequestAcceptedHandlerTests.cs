using FakeItEasy;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.Spirometry.Core.Constants;
using Signify.Spirometry.Core.EventHandlers.Akka;
using Signify.Spirometry.Core.Events;
using Signify.Spirometry.Core.FeatureFlagging;
using Signify.Spirometry.Core.Filters;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.Spirometry.Core.Tests.EventHandlers.Akka;

public class BillRequestAcceptedHandlerTests
{
    private readonly ILogger<BillRequestAcceptedHandler> _logger = A.Fake<ILogger<BillRequestAcceptedHandler>>();
    
    private readonly BillRequestAcceptedHandler _billRequestAcceptedHandler;
    private readonly TestableEndpointInstance _fakeMessageSession = new();
    private readonly IProductFilter _productFilter = A.Dummy<IProductFilter>();
    private readonly IFeatureFlags _featureFlags = A.Fake<IFeatureFlags>();
    
    public BillRequestAcceptedHandlerTests()
    {
        _billRequestAcceptedHandler = new BillRequestAcceptedHandler(_logger, _fakeMessageSession, _productFilter, _featureFlags);
    }
    
    [Fact]
    public async Task Handle_WhenProductCodeCorrect()
    {
        A.CallTo(() => _featureFlags.EnableBillAccepted).Returns(true);
        A.CallTo(() => _productFilter.ShouldProcess(A<string>._)).Returns(true);
        var @event = new BillRequestAccepted
        {
            RCMProductCode = ProductCodes.Spirometry
        };
        
        await _billRequestAcceptedHandler.Handle(@event, CancellationToken.None);
        
        // Verify Bill accepted status is checked
        A.CallTo(() => _featureFlags.EnableBillAccepted).MustHaveHappened(1, Times.Exactly); 
        
        Assert.NotNull(_fakeMessageSession.FindSentMessage<BillRequestAccepted>());
    }

    [Fact]
    public async Task Handle_WhenProductCodeIsMissing()
    {
        A.CallTo(() => _featureFlags.EnableBillAccepted).Returns(true);
        var @event = new BillRequestAccepted
        {
            RCMProductCode = string.Empty
        };
        await _billRequestAcceptedHandler.Handle(@event, CancellationToken.None);
        
        // Verify Bill accepted status is checked
        A.CallTo(() => _featureFlags.EnableBillAccepted).MustHaveHappened(1, Times.Exactly);

        Assert.Null(_fakeMessageSession.FindSentMessage<BillRequestAccepted>());
    }
}