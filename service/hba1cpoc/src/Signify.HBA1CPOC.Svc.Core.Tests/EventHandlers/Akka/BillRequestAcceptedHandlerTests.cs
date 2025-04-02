using FakeItEasy;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.HBA1CPOC.Svc.Core.Constants;
using Signify.HBA1CPOC.Svc.Core.EventHandlers.Akka;
using Signify.HBA1CPOC.Svc.Core.Events;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.EventHandlers.Akka;

public class BillRequestAcceptedHandlerTests
{
    private readonly ILogger<BillRequestAcceptedHandler> _logger = A.Fake<ILogger<BillRequestAcceptedHandler>>();
    
    private readonly BillRequestAcceptedHandler _billRequestAcceptedHandler;
    private readonly TestableEndpointInstance _fakeMessageSession = new();

    public BillRequestAcceptedHandlerTests()
    {
        _billRequestAcceptedHandler = new BillRequestAcceptedHandler(
            _logger,
            _fakeMessageSession);
    }
    
    [Fact]
    public async Task Handle_WhenProductCodeCorrect()
    {
        var @event = new BillRequestAccepted
        {
            RCMProductCode = ApplicationConstants.ProductCode,
        };
        
        await _billRequestAcceptedHandler.Handle(@event, CancellationToken.None);
        
        Assert.NotNull(_fakeMessageSession.FindSentMessage<BillRequestAccepted>());
    }

    [Fact]
    public async Task Handle_WhenProductCodeIsMissing()
    {
        var @event = new BillRequestAccepted
        {
            RCMProductCode = string.Empty
        };
        await _billRequestAcceptedHandler.Handle(@event, CancellationToken.None);

        Assert.Null(_fakeMessageSession.FindSentMessage<BillRequestAccepted>());
    }
}