using FakeItEasy;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.PAD.Svc.Core.Constants;
using Signify.PAD.Svc.Core.EventHandlers.Akka;
using Signify.PAD.Svc.Core.Events;
using Signify.PAD.Svc.Core.Filters;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.EventHandlers.Akka;

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
    
    [Fact]
    public async Task Handle_WhenProductCodeCorrect()
    {
        // Arrange
        A.CallTo(() => _productFilter.ShouldProcess(A<string>._)).Returns(true);
        var @event = new BillRequestAccepted
        {
            RCMProductCode = Application.ProductCode
        };
        
        // Act
        await _handler.Handle(@event, CancellationToken.None);
        
        // Assert
        Assert.NotNull(_fakeMessageSession.FindSentMessage<BillRequestAccepted>());
    }

    [Fact]
    public async Task Handle_WhenProductCodeIsMissing()
    {
        // Arrange
        var @event = new BillRequestAccepted
        {
            RCMProductCode = string.Empty
        };
        
        // Act
        await _handler.Handle(@event, CancellationToken.None);
        
        // Assert
        Assert.Null(_fakeMessageSession.FindSentMessage<BillRequestAccepted>());
    }
}