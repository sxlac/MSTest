using FakeItEasy;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.FOBT.Svc.Core.Constants;
using Signify.FOBT.Svc.Core.EventHandlers.Akka;
using Signify.FOBT.Svc.Core.Events;
using Signify.FOBT.Svc.Core.Filters;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.EventHandlers.Akka;

public class BillRequestAcceptedHandlerTests
{
    private readonly ILogger<BillRequestAcceptedHandler> _logger = A.Fake<ILogger<BillRequestAcceptedHandler>>();
    
    private readonly BillRequestAcceptedHandler _handler;
    private readonly TestableEndpointInstance _fakeMessageSession = new();
    private readonly IProductFilter _productFilter = A.Dummy<IProductFilter>();
    
    public BillRequestAcceptedHandlerTests()
    {
        _handler = new BillRequestAcceptedHandler(_logger, _fakeMessageSession, _productFilter);
    }
    
    [Theory]
    [InlineData(ApplicationConstants.BILLING_PRODUCT_CODE_RESULTS)]
    [InlineData(ApplicationConstants.BILLING_PRODUCT_CODE_LEFT)]
    [InlineData(ApplicationConstants.PRODUCT_CODE)]
    public async Task Handle_WhenProductCodeCorrect(string productCode)
    {
        A.CallTo(() => _productFilter.ShouldProcessBilling(A<string>._)).Returns(true);
        var @event = new BillRequestAccepted
        {
            RCMProductCode = productCode
        };
        
        await _handler.Handle(@event, CancellationToken.None);
        
        Assert.NotNull(_fakeMessageSession.FindSentMessage<BillRequestAccepted>());
    }

    [Fact]
    public async Task Handle_WhenProductCodeIsMissing()
    {
        var @event = new BillRequestAccepted
        {
            RCMProductCode = string.Empty
        };
        await _handler.Handle(@event, CancellationToken.None);
        

        Assert.Null(_fakeMessageSession.FindSentMessage<BillRequestAccepted>());
    }
}