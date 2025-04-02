using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.Dps.Observability.Library.Services;
using Signify.PAD.Svc.Core.EventHandlers;
using Signify.PAD.Svc.Core.Events;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.EventHandlers;

public class PdfDeliveredHandlerTests
{
    private readonly TestableEndpointInstance _endpointInstance;
    private readonly PdfDeliveredHandler _handler;

    public PdfDeliveredHandlerTests()
    {
        _endpointInstance = new TestableEndpointInstance();
        var publishObservability = A.Fake<IPublishObservability>();
        _handler = new PdfDeliveredHandler(A.Dummy<ILogger<PdfDeliveredHandler>>(), _endpointInstance, publishObservability);
    }

    [Fact]
    public async Task Handle_WithNonPadProduct_IsIgnored()
    {
        var @event = new PdfDeliveredToClient
        {
            ProductCodes = new List<string> { "Spirometry" }
        };

        await _handler.Handle(@event, CancellationToken.None);

        _endpointInstance.SentMessages.Length.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithPadProduct_IsProcessed()
    {
        var @event = new PdfDeliveredToClient
        {
            ProductCodes = new List<string> { "PAD" }
        };

        await _handler.Handle(@event, CancellationToken.None);

        _endpointInstance.SentMessages.Length.Should().Be(1);
    }
}